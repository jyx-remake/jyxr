using Game.Core.Affix;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    internal int ApplyDirectDamage(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        int amount,
        HookTiming? timing = null,
        string? detail = null) =>
        _damageResolver.Apply(
            state,
            source,
            target,
            amount,
            runBeforeDamageApplied: false,
            eventTiming: timing,
            detail: detail).ActualAmount;

    internal BattleHookContext TriggerHooks(
        BattleState state,
        HookTiming timing,
        BattleUnit unit,
        Action<BattleHookContext>? configure = null,
        BattleHookExecutionMode executionMode = BattleHookExecutionMode.Execute,
        bool recordEvents = true,
        Func<HookAffix, bool>? hookFilter = null)
        => _hookRunner.Run(state, timing, unit, configure, executionMode, recordEvents, hookFilter);

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

}
