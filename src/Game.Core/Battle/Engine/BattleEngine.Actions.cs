using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    public BattleCommandResult<BattleActionResult> CastSkill(BattleState state, string unitId, SkillInstance skill, GridPosition target)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(skill);
        using var command = state.BeginCommand();

        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: true);
        if (!validation.Success)
        {
            return BattleCommandResult<BattleActionResult>.Failed(validation.Message, command.Messages);
        }

        var unit = state.GetUnit(unitId);
        if (!ReferenceEquals(skill.Owner, unit.Character))
        {
            return BattleCommandResult<BattleActionResult>.Failed("Skill does not belong to acting unit.", command.Messages);
        }

        if (!skill.IsActive)
        {
            return BattleCommandResult<BattleActionResult>.Failed("Skill is not active.", command.Messages);
        }

        var availability = EvaluateSkillAvailabilityCore(state, unit, skill);
        if (!availability.IsAvailable)
        {
            return BattleCommandResult<BattleActionResult>.Failed(GetSkillUnavailableMessage(availability), command.Messages);
        }

        var mpCost = ResolveSkillMpCostExecute(state, unit, skill);
        if (unit.Mp < mpCost)
        {
            return BattleCommandResult<BattleActionResult>.Failed("Not enough MP.", command.Messages);
        }

        var resolvedSkill = _legendSkillResolver.Resolve(_legendSkillsProvider(), skill, _random);
        var resolvedSpecialSkill = resolvedSkill as SpecialSkillInstance;
        var skillCastInfo = BattleSkillCastInfo.Create(skill, resolvedSkill);

        if (unit.Rage < resolvedSkill.RageCost)
        {
            return BattleCommandResult<BattleActionResult>.Failed("Not enough rage.", command.Messages);
        }

        var castSize = BattleSkillTargeting.ResolveEffectiveCastSize(unit, resolvedSkill);
        var impactSize = BattleSkillTargeting.ResolveEffectiveImpactSize(unit, resolvedSkill);
        if (unit.Position.ManhattanDistanceTo(target) > castSize)
        {
            return BattleCommandResult<BattleActionResult>.Failed("Target is out of cast range.", command.Messages);
        }

        TriggerHooks(state, HookTiming.BeforeSkillCast, unit, context =>
        {
            context.Source = unit;
            context.Skill = resolvedSkill;
        });

        if (resolvedSpecialSkill is not null)
        {
            TryRequestSpecialSkillSpeech(state, unit, resolvedSpecialSkill);
        }

        unit.SpendMp(mpCost);
        unit.SpendRage(resolvedSkill.RageCost);
        resolvedSkill.CurrentCooldown = resolvedSkill.Cooldown;
        UpdateFacingByTarget(unit, target);

        var impactedPositions = ResolveImpactPositions(unit.Position, target, resolvedSkill.ImpactType, impactSize)
            .Where(state.Grid.Contains)
            .ToHashSet();
        var targets = BattleSkillTargeting.ResolveEffectiveTargets(state, unit, resolvedSkill, impactedPositions);

        var battleEvent = new BattleFact(
            BattleFactKind.SkillCast,
            unit.Id,
            detail: resolvedSkill.Id,
            skillCast: skillCastInfo);
        AddMessage(state, battleEvent);
        _skillExecutor.Execute(
            state,
            unit,
            targets,
            BattleSkillExecutionPlanFactory.Create(resolvedSkill));
        TriggerHooks(state, HookTiming.AfterSkillCast, unit, context =>
        {
            context.Source = unit;
            context.Skill = resolvedSkill;
        });
        _growthResolver.ApplySkillUseGrowth(state, unit, skill);
        unit.RecordUsedSkill(skill.Id);
        EndActionCore(state, unit, committedMainAction: true);
        return BattleCommandResult<BattleActionResult>.Succeeded(
            new BattleActionResult(
                targets.Select(static targetUnit => targetUnit.Id).ToList(),
                impactedPositions.OrderBy(static position => position.Y).ThenBy(static position => position.X).ToList(),
                skillCastInfo),
            command.Messages);
    }

    private void TryRequestSpecialSkillSpeech(
        BattleState state,
        BattleUnit source,
        SpecialSkillInstance specialSkill)
    {
        var line = BattleSpeechRuntime.TryPickLine(specialSkill.Definition.Speech, _random);
        BattleSpeechRuntime.TryEmit(state, source, line);
    }

    private static string GetSkillUnavailableMessage(BattleSkillAvailability availability) =>
        availability.Status switch
        {
            BattleSkillAvailabilityStatus.Cooldown => "Skill is disabled.",
            BattleSkillAvailabilityStatus.Disabled => "Skill is disabled.",
            BattleSkillAvailabilityStatus.NotEnoughMp => "Not enough MP.",
            BattleSkillAvailabilityStatus.NotEnoughRage => "Not enough rage.",
            _ => "Skill is not available.",
        };

    public BattleCommandResult<BattleActionResult> UseItem(
        BattleState state,
        string unitId,
        ItemDefinition item,
        string targetUnitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetUnitId);
        using var command = state.BeginCommand();

        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: true);
        if (!validation.Success)
        {
            return BattleCommandResult<BattleActionResult>.Failed(validation.Message, command.Messages);
        }

        var unit = state.GetUnit(unitId);
        var target = state.TryGetUnit(targetUnitId);
        if (target is null || !target.IsAlive)
        {
            return BattleCommandResult<BattleActionResult>.Failed("Invalid item target.", command.Messages);
        }

        if (target.Id != unit.Id)
        {
            if (state.AreEnemies(unit, target))
            {
                return BattleCommandResult<BattleActionResult>.Failed("Items cannot target enemies.", command.Messages);
            }

            if (!unit.HasTrait(TraitId.CanUseItemOnAlly))
            {
                return BattleCommandResult<BattleActionResult>.Failed("Unit cannot use items on allies.", command.Messages);
            }

            if (unit.Position.ManhattanDistanceTo(target.Position) > 2)
            {
                return BattleCommandResult<BattleActionResult>.Failed("Ally item target is out of range.", command.Messages);
            }
        }
        var useItemCooldown = IsItemCooldownEnabled(state.RuleSettings);
        if (useItemCooldown && target.ItemCooldown > 0 && !unit.HasTrait(TraitId.IgnoreItemCooldown))
        {
            return BattleCommandResult<BattleActionResult>.Failed($"Item is cooling down. Remaining turns: {target.ItemCooldown}.", command.Messages);
        }

        TriggerHooks(state, HookTiming.BeforeItemUse, unit);
        ApplyItemEffects(state, unit, target, item.UseEffects);
        if (useItemCooldown)
        {
            target.AddItemCooldown(item.Cooldown);
        }
        UpdateFacingByTarget(unit, target.Position);

        var battleEvent = new BattleFact(BattleFactKind.ItemUsed, unit.Id, detail: item.Id);
        AddMessage(state, battleEvent);
        TriggerHooks(state, HookTiming.AfterItemUse, unit);
        EndActionCore(state, unit, committedMainAction: true);
        return BattleCommandResult<BattleActionResult>.Succeeded(
            new BattleActionResult([target.Id], []), command.Messages, "Item used.");
    }

    private static bool IsItemCooldownEnabled(BattleRuleSettings ruleSettings) =>
        !ruleSettings.EnableDifficultyItemCooldownRules ||
        ruleSettings.Difficulty != GameDifficulty.Normal;

    public BattleCommandResult<BattleActionResult> Rest(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        using var command = state.BeginCommand();
        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: true);
        if (!validation.Success)
        {
            return BattleCommandResult<BattleActionResult>.Failed(validation.Message, command.Messages);
        }

        var unit = state.GetUnit(unitId);
        TriggerHooks(state, HookTiming.BeforeRest, unit);

        var recovery = BattleRestCalculator.Roll(unit, _random);
        var restoredHp = _recoveryResolver.Apply(
            state,
            unit,
            unit,
            BattleRecoveryKind.Hp,
            recovery.Hp).ActualAmount;
        var restoredMp = _recoveryResolver.Apply(
            state,
            unit,
            unit,
            BattleRecoveryKind.Mp,
            recovery.Mp).ActualAmount;

        var battleEvent = new BattleFact(
            BattleFactKind.Rested,
            unit.Id,
            rest: new BattleRestRecovery(restoredHp, restoredMp));
        AddMessage(state, battleEvent);
        TriggerHooks(state, HookTiming.AfterRest, unit);
        EndActionCore(state, unit, committedMainAction: true);
        return BattleCommandResult<BattleActionResult>.Succeeded(
            new BattleActionResult([unit.Id], []), command.Messages);
    }

    public BattleCommandResult<BattleActionResult> EndAction(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        using var command = state.BeginCommand();
        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: false);
        if (!validation.Success)
        {
            return BattleCommandResult<BattleActionResult>.Failed(validation.Message, command.Messages);
        }

        var unit = state.GetUnit(unitId);
        EndActionCore(state, unit, committedMainAction: false);
        return BattleCommandResult<BattleActionResult>.Succeeded(
            new BattleActionResult([unit.Id], []), command.Messages, "Action ended.");
    }
}
