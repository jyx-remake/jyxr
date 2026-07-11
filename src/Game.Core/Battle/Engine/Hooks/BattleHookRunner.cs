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

        RunHooks(
            context,
            unit.Character.GetHooks(timing).Where(MatchesFilter).ToList(),
            recordEvents);

        foreach (var buff in unit.GetActiveBuffs())
        {
            var hooks = buff.Definition.Affixes
                .OfType<HookAffix>()
                .Where(hook => hook.Timing == timing)
                .Where(MatchesFilter)
                .ToList();
            if (hooks.Count == 0)
            {
                continue;
            }

            var previousBuff = context.Buff;
            var previousSource = context.Source;
            context.Buff = buff;
            context.Source ??= state.TryGetUnit(buff.SourceUnitId);
            RunHooks(context, hooks, recordEvents);
            context.Buff = previousBuff;
            context.Source = previousSource;
        }

        return context;

        bool MatchesFilter(HookAffix hook) => hookFilter is null || hookFilter(hook);
    }

    private void RunHooks(
        BattleHookContext context,
        IReadOnlyList<HookAffix> hooks,
        bool recordEvents)
    {
        if (hooks.Count == 0)
        {
            return;
        }

        if (recordEvents)
        {
            context.State.AddMessage(new BattleTrace(
                BattleTraceKind.HooksTriggered,
                context.Unit.Id,
                context.Timing,
                BuildHookLabels(hooks),
                context.State.CurrentExecutionScope));
        }

        foreach (var hook in hooks)
        {
            executor.Execute(context, hook);
        }
    }

    private static IReadOnlyList<string> BuildHookLabels(IReadOnlyList<HookAffix> hooks) =>
        hooks.Select(hook => hook.Effects.Count == 0
            ? hook.Timing.ToString()
            : $"{hook.Timing}:{string.Join('+', hook.Effects.Select(static effect => effect.GetType().Name
                .Replace("BattleHookEffectDefinition", string.Empty, StringComparison.Ordinal)
                .Replace("BattleEffectDefinition", string.Empty, StringComparison.Ordinal)))}")
            .ToList();
}
