using Game.Core.Abstractions;
using Game.Core.Affix;

namespace Game.Core.Battle;

internal delegate BattleHookContext BattleHookTrigger(
    BattleState state,
    HookTiming timing,
    BattleUnit unit,
    Action<BattleHookContext>? configure = null);

internal sealed class BattleHookRunner(
    BattleEngine engine,
    BattleHookExecutor executor,
    IRandomService random)
{
    public BattleHookContext Run(
        BattleState state,
        HookTiming timing,
        BattleUnit unit,
        Action<BattleHookContext>? configure = null,
        BattleHookExecutionMode executionMode = BattleHookExecutionMode.Execute,
        bool recordEvents = true,
        Func<HookAffix, bool>? hookFilter = null)
    {
        var context = new BattleHookContext(engine, state, timing, unit, random, executionMode);
        configure?.Invoke(context);

        var entries = state.ProjectionResolver.GetHooks(unit, timing)
            .Where(entry => MatchesFilter(entry.Hook))
            .Where(entry => MatchesPeriodicBuff(entry, context.Buff, timing))
            .ToList();

        RunHooks(
            context,
            entries
                .OrderByDescending(static entry => entry.Hook.Priority)
                .ThenBy(static entry => entry.Origin.LayerOrder)
                .ThenBy(entry => entry.Provider is null ? int.MaxValue : FindUnitOrder(state, entry.Provider))
                .ThenBy(static entry => entry.SourceSequence)
                .ThenBy(static entry => entry.AffixOrder)
                .ToList(),
            recordEvents);

        return context;

        bool MatchesFilter(HookAffix hook) => hookFilter is null || hookFilter(hook);

        static bool MatchesPeriodicBuff(
            ActiveHookEntry entry,
            BattleBuffInstance? eventBuff,
            HookTiming currentTiming) =>
            currentTiming != HookTiming.AfterBuffRound ||
            eventBuff is null ||
            entry.Origin is not BuffAffixOrigin origin ||
            string.Equals(origin.BuffId, eventBuff.Definition.Id, StringComparison.Ordinal) &&
            origin.AppliedAtActionSerial == eventBuff.AppliedAtActionSerial;
    }

    private void RunHooks(
        BattleHookContext context,
        IReadOnlyList<ActiveHookEntry> entries,
        bool recordEvents)
    {
        if (entries.Count == 0)
        {
            return;
        }

        if (recordEvents)
        {
            context.State.AddMessage(new BattleTrace(
                BattleTraceKind.HooksTriggered,
                context.Unit.Id,
                context.Timing,
                BuildHookLabels(entries),
                context.State.CurrentExecutionScope));
        }

        foreach (var entry in entries)
        {
            if (context.Timing == HookTiming.BeforeDefeated && context.IsDefeatPrevented)
            {
                break;
            }

            var previousBuff = context.Buff;
            var previousProvider = context.Provider;
            context.Provider = entry.Provider;
            if (entry.Origin is BuffAffixOrigin buffOrigin)
                context.Buff = context.Unit.Buffs.FirstOrDefault(buff =>
                    buff.Definition.Id == buffOrigin.BuffId &&
                    buff.AppliedAtActionSerial == buffOrigin.AppliedAtActionSerial);

            executor.Execute(context, entry.Hook);
            context.Buff = previousBuff;
            context.Provider = previousProvider;
        }
    }

    private static IReadOnlyList<string> BuildHookLabels(IReadOnlyList<ActiveHookEntry> hooks) =>
        hooks.Select(entry => entry.Hook.Effects.Count == 0
            ? entry.Hook.Timing.ToString()
            : $"{entry.Hook.Timing}:{string.Join('+', entry.Hook.Effects.Select(static effect => effect.GetType().Name
                .Replace("BattleHookEffectDefinition", string.Empty, StringComparison.Ordinal)
                .Replace("BattleEffectDefinition", string.Empty, StringComparison.Ordinal)))}")
            .ToList();

    private static int FindUnitOrder(BattleState state, BattleUnit unit)
    {
        for (var index = 0; index < state.Units.Count; index++)
            if (ReferenceEquals(state.Units[index], unit)) return index;
        return int.MaxValue;
    }
}
