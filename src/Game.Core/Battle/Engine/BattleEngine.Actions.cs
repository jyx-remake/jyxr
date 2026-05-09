using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    public BattleActionResult CastSkill(BattleState state, string unitId, SkillInstance skill, GridPosition target)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(skill);

        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: true);
        if (!validation.Success)
        {
            return validation;
        }

        var unit = state.GetUnit(unitId);
        if (!ReferenceEquals(skill.Owner, unit.Character))
        {
            return BattleActionResult.Failed("Skill does not belong to acting unit.");
        }

        if (!skill.IsActive)
        {
            return BattleActionResult.Failed("Skill is not active.");
        }

        var availability = EvaluateSkillAvailabilityCore(state, unit, skill);
        if (!availability.IsAvailable)
        {
            return BattleActionResult.Failed(GetSkillUnavailableMessage(availability));
        }

        var mpCost = ResolveSkillMpCostExecute(state, unit, skill);
        if (unit.Mp < mpCost)
        {
            return BattleActionResult.Failed("Not enough MP.");
        }

        var resolvedSkill = _legendSkillResolver.Resolve(_legendSkillsProvider(), skill, _random);
        var skillCastInfo = BattleSkillCastInfo.Create(skill, resolvedSkill);

        if (unit.Rage < resolvedSkill.RageCost)
        {
            return BattleActionResult.Failed("Not enough rage.");
        }

        if (unit.Position.ManhattanDistanceTo(target) > skill.CastSize)
        {
            return BattleActionResult.Failed("Target is out of cast range.");
        }

        TriggerHooks(state, HookTiming.BeforeSkillCast, unit, context =>
        {
            context.Source = unit;
            context.Skill = resolvedSkill;
        });

        unit.SpendMp(mpCost);
        unit.SpendRage(resolvedSkill.RageCost);
        resolvedSkill.CurrentCooldown = resolvedSkill.Cooldown;
        UpdateFacingByTarget(unit, target);

        var impactedPositions = ResolveImpactPositions(unit.Position, target, resolvedSkill.ImpactType, resolvedSkill.ImpactSize)
            .Where(state.Grid.Contains)
            .ToHashSet();
        var targets = BattleSkillTargeting.ResolveEffectiveTargets(state, unit, resolvedSkill, impactedPositions);

        foreach (var targetUnit in targets)
        {
            var damage = ApplySkillDamage(state, unit, targetUnit, resolvedSkill);
            TryGainRageFromTakingDamage(state, unit, targetUnit, damage);
            ApplySkillBuffs(state, unit, targetUnit, resolvedSkill.Buffs);
        }

        if (targets.Any(targetUnit => state.AreEnemies(unit, targetUnit)))
        {
            TryGainRageFromAttack(state, unit);
        }

        var battleEvent = new BattleEvent(
            BattleEventKind.SkillCast,
            unit.Id,
            Detail: resolvedSkill.Id,
            SkillCast: skillCastInfo);
        AddEvent(state, battleEvent);
        TriggerHooks(state, HookTiming.AfterSkillCast, unit, context =>
        {
            context.Source = unit;
            context.Skill = resolvedSkill;
        });
        EndActionCore(state, unit, committedMainAction: true);
        return BattleActionResult.Succeeded(
            string.Empty,
            targets.Select(static targetUnit => targetUnit.Id).ToList(),
            [battleEvent],
            impactedPositions.OrderBy(static position => position.Y).ThenBy(static position => position.X).ToList(),
            skillCastInfo);
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

    public BattleActionResult UseItem(
        BattleState state,
        string unitId,
        ItemDefinition item,
        string targetUnitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetUnitId);

        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: true);
        if (!validation.Success)
        {
            return validation;
        }

        var unit = state.GetUnit(unitId);
        var target = state.TryGetUnit(targetUnitId);
        if (target is null || !target.IsAlive)
        {
            return BattleActionResult.Failed("Invalid item target.");
        }

        if (target.Id != unit.Id)
        {
            if (state.AreEnemies(unit, target))
            {
                return BattleActionResult.Failed("Items cannot target enemies.");
            }

            if (!unit.HasTrait(TraitId.CanUseItemOnAlly))
            {
                return BattleActionResult.Failed("Unit cannot use items on allies.");
            }

            if (unit.Position.ManhattanDistanceTo(target.Position) > 2)
            {
                return BattleActionResult.Failed("Ally item target is out of range.");
            }
        }
        if (target.ItemCooldown > 0 && !unit.HasTrait(TraitId.IgnoreItemCooldown))
        {
            return BattleActionResult.Failed($"Item is cooling down. Remaining turns: {target.ItemCooldown}.");
        }

        TriggerHooks(state, HookTiming.BeforeItemUse, unit);
        ApplyItemEffects(state, unit, target, item.UseEffects);
        target.AddItemCooldown(item.Cooldown);
        UpdateFacingByTarget(unit, target.Position);

        var battleEvent = new BattleEvent(BattleEventKind.ItemUsed, unit.Id, Detail: item.Id);
        AddEvent(state, battleEvent);
        TriggerHooks(state, HookTiming.AfterItemUse, unit);
        EndActionCore(state, unit, committedMainAction: true);
        return BattleActionResult.Succeeded("Item used.", [target.Id], [battleEvent]);
    }

    public BattleActionResult Rest(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: true);
        if (!validation.Success)
        {
            return validation;
        }

        var unit = state.GetUnit(unitId);
        TriggerHooks(state, HookTiming.BeforeRest, unit);

        var recovery = BattleRestCalculator.Roll(unit, _random);
        var restoredHp = unit.RestoreHp(recovery.Hp);
        var restoredMp = unit.RestoreMp(recovery.Mp);

        var battleEvent = new BattleEvent(
            BattleEventKind.Rested,
            unit.Id,
            Rest: new BattleRestRecovery(restoredHp, restoredMp));
        AddEvent(state, battleEvent);
        TriggerHooks(state, HookTiming.AfterRest, unit);
        EndActionCore(state, unit, committedMainAction: true);
        return BattleActionResult.Succeeded(string.Empty, [unit.Id], [battleEvent]);
    }

    public BattleActionResult EndAction(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        var validation = ValidateActingUnit(state, unitId, requireMainActionAvailable: false);
        if (!validation.Success)
        {
            return validation;
        }

        var unit = state.GetUnit(unitId);
        EndActionCore(state, unit, committedMainAction: false);
        return BattleActionResult.Succeeded("Action ended.", [unit.Id]);
    }
}
