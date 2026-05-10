using Game.Core.Affix;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    private BattleHookContext TriggerHooks(
        BattleState state,
        HookTiming timing,
        BattleUnit unit,
        Action<BattleHookContext>? configure = null,
        BattleHookExecutionMode executionMode = BattleHookExecutionMode.Execute,
        bool recordEvents = true,
        Func<HookAffix, bool>? hookFilter = null)
    {
        var context = CreateHookContext(state, timing, unit, executionMode);
        configure?.Invoke(context);
        var hooks = unit.Character.GetHooks(timing)
            .Where(hook => hookFilter is null || hookFilter(hook))
            .ToList();
        if (hooks.Count > 0)
        {
            if (recordEvents)
            {
                AddEvent(state, new BattleEvent(BattleEventKind.HooksTriggered, unit.Id, timing, BuildHookLabels(hooks)));
            }

            foreach (var hook in hooks)
            {
                _hookExecutor.Execute(context, hook);
            }
        }

        foreach (var buff in unit.GetActiveBuffs())
        {
            var buffHooks = buff.Definition.Affixes
                .OfType<HookAffix>()
                .Where(hook => hook.Timing == timing)
                .Where(hook => hookFilter is null || hookFilter(hook))
                .ToList();
            if (buffHooks.Count == 0)
            {
                continue;
            }

            if (recordEvents)
            {
                AddEvent(state, new BattleEvent(BattleEventKind.HooksTriggered, unit.Id, timing, BuildHookLabels(buffHooks)));
            }

            var previousBuff = context.Buff;
            var previousSource = context.Source;
            context.Buff = buff;
            context.Source ??= state.TryGetUnit(buff.SourceUnitId);
            foreach (var hook in buffHooks)
            {
                _hookExecutor.Execute(context, hook);
            }
            context.Buff = previousBuff;
            context.Source = previousSource;
        }

        return context;
    }

    private int ResolveSkillMpCostPreview(BattleState state, BattleUnit unit, SkillInstance skill) =>
        ResolveSkillMpCost(state, unit, skill, BattleHookExecutionMode.Preview);

    private int ResolveSkillMpCostExecute(BattleState state, BattleUnit unit, SkillInstance skill) =>
        ResolveSkillMpCost(state, unit, skill, BattleHookExecutionMode.Execute);

    private int ResolveSkillMpCost(
        BattleState state,
        BattleUnit unit,
        SkillInstance skill,
        BattleHookExecutionMode executionMode)
    {
        var context = TriggerHooks(
            state,
            HookTiming.BeforeSkillCost,
            unit,
            hookContext =>
            {
                hookContext.Skill = skill;
                hookContext.MpCost = skill.MpCost;
            },
            executionMode,
            recordEvents: false);
        return Math.Max(0, context.MpCost ?? skill.MpCost);
    }

    private BattleHookContext CreateHookContext(
        BattleState state,
        HookTiming timing,
        BattleUnit unit,
        BattleHookExecutionMode executionMode) =>
        new(this, state, timing, unit, _random, executionMode);

    private static IReadOnlyList<string> BuildHookLabels(IReadOnlyList<HookAffix> hooks) =>
        hooks.Select(hook => hook.Effects.Count == 0
            ? hook.Timing.ToString()
            : $"{hook.Timing}:{string.Join('+', hook.Effects.Select(static effect => effect.GetType().Name
                .Replace("BattleHookEffectDefinition", string.Empty, StringComparison.Ordinal)
                .Replace("BattleEffectDefinition", string.Empty, StringComparison.Ordinal)))}")
            .ToList();
}
