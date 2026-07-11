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

        var entries = new List<HookExecutionEntry>();
        var sequence = 0;
        entries.AddRange(unit.Character.GetHooks(timing)
            .Where(MatchesFilter)
            .Select(hook => new HookExecutionEntry(hook, null, sequence++)));

        foreach (var buff in unit.GetActiveBuffs())
        {
            entries.AddRange(buff.Definition.Affixes
                .OfType<HookAffix>()
                .Where(hook => hook.Timing == timing)
                .Where(MatchesFilter)
                .Select(hook => new HookExecutionEntry(hook, buff, sequence++)));
        }

        RunHooks(
            context,
            entries
                .OrderByDescending(static entry => entry.Hook.Priority)
                .ThenBy(static entry => entry.Sequence)
                .ToList(),
            recordEvents);

        return context;

        bool MatchesFilter(HookAffix hook) => hookFilter is null || hookFilter(hook);
    }

    private void RunHooks(
        BattleHookContext context,
        IReadOnlyList<HookExecutionEntry> entries,
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
                BuildHookLabels(entries.Select(static entry => entry.Hook).ToList()),
                context.State.CurrentExecutionScope));
        }

        foreach (var entry in entries)
        {
            if (context.Timing == HookTiming.BeforeDefeated && context.IsDefeatPrevented)
            {
                break;
            }

            var previousBuff = context.Buff;
            var previousSource = context.Source;
            if (entry.Buff is not null)
            {
                context.Buff = entry.Buff;
                context.Source ??= context.State.TryGetUnit(entry.Buff.SourceUnitId);
            }

            executor.Execute(context, entry.Hook);
            context.Buff = previousBuff;
            context.Source = previousSource;
        }
    }

    private sealed record HookExecutionEntry(HookAffix Hook, BattleBuffInstance? Buff, int Sequence);

    private static IReadOnlyList<string> BuildHookLabels(IReadOnlyList<HookAffix> hooks) =>
        hooks.Select(hook => hook.Effects.Count == 0
            ? hook.Timing.ToString()
            : $"{hook.Timing}:{string.Join('+', hook.Effects.Select(static effect => effect.GetType().Name
                .Replace("BattleHookEffectDefinition", string.Empty, StringComparison.Ordinal)
                .Replace("BattleEffectDefinition", string.Empty, StringComparison.Ordinal)))}")
            .ToList();
}
