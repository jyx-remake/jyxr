using Game.Core.Affix;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    public BattleActionContext BeginAction(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);

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
        AddEvent(state, new BattleEvent(BattleEventKind.ActionStarted, unit.Id));
        TriggerHooks(state, HookTiming.BeforeActionStart, unit);
        return context;
    }

    public BattleUnit AdvanceUntilNextAction(BattleState state, int maxTicks = 100_000)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTicks, 1);

        if (state.CurrentAction is not null)
        {
            throw new InvalidOperationException("Cannot advance timeline while a unit is acting.");
        }

        for (var i = 0; i < maxTicks; i++)
        {
            var ready = SelectReadyUnit(state);
            if (ready is not null)
            {
                BeginAction(state, ready.Id);
                return ready;
            }

            state.Tick++;
            AdvanceTimelineTick(state);
        }

        throw new InvalidOperationException("No unit became actionable before maxTicks was reached.");
    }

    private static BattleUnit? SelectReadyUnit(BattleState state) =>
        state.GetLivingUnits()
            .Where(static unit => unit.ActionGauge >= ActionGaugeThreshold)
            .OrderByDescending(static unit => unit.ActionGauge)
            .ThenByDescending(static unit => unit.ActionSpeed)
            .ThenBy(static unit => unit.Id, StringComparer.Ordinal)
            .FirstOrDefault();

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
            }

            foreach (var skill in unit.Character.GetInternalSkills())
            {
                if (skill.CurrentCooldown > 0)
                {
                    skill.CurrentCooldown--;
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
