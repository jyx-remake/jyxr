using Game.Core.Affix;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    public BattleCommandResult<BattleActionContext?> BeginAction(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);
        using var command = state.BeginCommand();

        if (TryGetBeginActionFailure(state, unitId) is { } failure)
        {
            return BattleCommandResult<BattleActionContext?>.Failed(failure, command.Messages);
        }

        var context = BeginActionCore(state, unitId);
        return BattleCommandResult<BattleActionContext?>.Succeeded(context, command.Messages);
    }

    private static string? TryGetBeginActionFailure(BattleState state, string unitId)
    {
        if (state.CurrentAction is not null) return "A unit is already acting.";
        var unit = state.GetUnit(unitId);
        if (!unit.IsAlive) return $"Unit '{unit.Id}' is defeated.";
        if (unit.ActionGauge < ActionGaugeThreshold) return $"Unit '{unit.Id}' is not ready to act.";
        return null;
    }

    private BattleActionContext? BeginActionCore(BattleState state, string unitId)
    {

        if (state.CurrentAction is not null)
        {
            throw new InvalidOperationException("A unit is already acting.");
        }

        var unit = state.GetUnit(unitId);
        if (!unit.IsAlive)
        {
            throw new InvalidOperationException($"Unit '{unit.Id}' is defeated.");
        }

        if (unit.ActionGauge < ActionGaugeThreshold)
        {
            throw new InvalidOperationException($"Unit '{unit.Id}' is not ready to act.");
        }

        var context = new BattleActionContext(unit);
        state.CurrentAction = context;
        state.ActionSerial++;
        TriggerHooks(state, HookTiming.BeforeActionReadiness, unit);
        if (unit.HasBuff(BattleContentIds.Stun))
        {
            SkipAction(state, unit, BattleContentIds.Stun);
            return null;
        }

        var hookContext = TriggerHooks(state, HookTiming.BeforeActionStart, unit);
        if (hookContext.IsActionSkipRequested)
        {
            SkipAction(state, unit, hookContext.ActionSkipReason);
            return null;
        }

        AddMessage(state, new BattleFact(BattleFactKind.ActionStarted, unit.Id));
        return context;
    }

    public BattleCommandResult<BattleUnit> AdvanceUntilNextAction(BattleState state, int maxTicks = 100_000)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTicks, 1);
        using var command = state.BeginCommand();

        if (state.CurrentAction is not null)
        {
            return BattleCommandResult<BattleUnit>.Failed(
                "Cannot advance timeline while a unit is acting.", command.Messages);
        }

        for (var i = 0; i < maxTicks; i++)
        {
            var ready = SelectReadyUnit(state);
            if (ready is not null)
            {
                if (BeginActionCore(state, ready.Id) is not null)
                {
                    return BattleCommandResult<BattleUnit>.Succeeded(ready, command.Messages);
                }

                continue;
            }

            state.Tick++;
            AdvanceTimelineTick(state);
        }

        return BattleCommandResult<BattleUnit>.Failed(
            "No unit became actionable before maxTicks was reached.", command.Messages);
    }

    private static BattleUnit? SelectReadyUnit(BattleState state) =>
        state.GetLivingUnits()
            .Where(static unit => unit.ActionGauge >= ActionGaugeThreshold)
            .OrderByDescending(static unit => unit.ActionGauge)
            .ThenByDescending(static unit => unit.ActionSpeed)
            .ThenBy(static unit => unit.Id, StringComparer.Ordinal)
            .FirstOrDefault();

    private static void SkipAction(BattleState state, BattleUnit unit, string? reason)
    {
        unit.ActionGauge = 0d;
        state.CurrentAction = null;
        state.AddMessage(new BattleFact(BattleFactKind.ActionSkipped, unit.Id, detail: reason));
    }

    private void AdvanceTimelineTick(BattleState state)
    {
        foreach (var unit in state.GetLivingUnits())
        {
            AdvanceBuffTimelines(state, unit);
        }

        if (state.Tick % TimelineTicksPerRound == 0)
        {
            RecoverCooldowns(state);
        }

        foreach (var unit in state.GetLivingUnits())
        {
            unit.ActionGauge += unit.ActionSpeed;
        }
    }

    private static void RecoverCooldowns(BattleState state)
    {
        foreach (var unit in state.Units)
        {
            foreach (var skill in unit.Character.GetExternalSkills())
            {
                if (skill.CurrentCooldown > 0)
                {
                    skill.CurrentCooldown--;
                }

                foreach (var formSkill in skill.GetFormSkills())
                {
                    if (formSkill.CurrentCooldown > 0)
                    {
                        formSkill.CurrentCooldown--;
                    }
                }
            }

            foreach (var skill in unit.Character.GetInternalSkills())
            {
                if (skill.CurrentCooldown > 0)
                {
                    skill.CurrentCooldown--;
                }

                foreach (var formSkill in skill.GetFormSkills())
                {
                    if (formSkill.CurrentCooldown > 0)
                    {
                        formSkill.CurrentCooldown--;
                    }
                }
            }

            foreach (var skill in unit.Character.GetSpecialSkills())
            {
                if (skill.CurrentCooldown > 0)
                {
                    skill.CurrentCooldown--;
                }
            }

            unit.RecoverItemCooldown();
        }
    }
}
