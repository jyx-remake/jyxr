using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Definitions;

namespace Game.Core.Battle;

internal sealed class BattleBuffResolver(
    BattleHookTrigger triggerHooks,
    IRandomService random,
    Func<string, BuffDefinition> definitionResolver)
{
    public BuffDefinition Resolve(ApplyBuffBattleEffectDefinition effect) =>
        effect.Buff ?? definitionResolver(effect.BuffId);

    public BuffDefinition Resolve(string buffId) => definitionResolver(buffId);

    public bool Apply(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        BuffDefinition definition,
        int level,
        int duration,
        string detail,
        HookTiming? timing = null)
    {
        if (definition.IsDebuff && target.HasBuff(BattleContentIds.HolyWar))
        {
            return false;
        }

        if (definition.IsDebuff && RollDebuffResistance(target))
        {
            state.AddMessage(new BattleFact(BattleFactKind.BuffResisted, target.Id, timing, detail: detail));
            return false;
        }

        var instance = new BattleBuffInstance(definition, level, duration, source.Id, state.ActionSerial);
        var hookContext = triggerHooks(state, HookTiming.BeforeBuffApplied, source, context =>
        {
            context.Source = source;
            context.Target = target;
            context.Buff = instance;
        });
        if (hookContext.Cancel)
        {
            return false;
        }

        target.ApplyBuff(instance);
        state.AddMessage(new BattleFact(BattleFactKind.BuffApplied, target.Id, timing, detail: detail));
        triggerHooks(state, HookTiming.OnBuffApplied, target, context =>
        {
            context.Source = source;
            context.Target = target;
            context.Buff = instance;
        });
        return true;
    }

    public IReadOnlyList<BattleBuffInstance> Remove(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        Func<BattleBuffInstance, bool> predicate,
        HookTiming? timing = null)
    {
        var removedBuffs = target.RemoveBuffs(predicate);
        NotifyRemoved(state, source, target, removedBuffs, timing);
        return removedBuffs;
    }

    public void NotifyRemoved(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        IReadOnlyList<BattleBuffInstance> removedBuffs,
        HookTiming? timing = null)
    {
        foreach (var removedBuff in removedBuffs)
        {
            state.AddMessage(new BattleFact(
                BattleFactKind.BuffRemoved,
                target.Id,
                timing,
                detail: removedBuff.Definition.Id));
            triggerHooks(state, HookTiming.OnBuffRemoved, target, context =>
            {
                context.Source = source;
                context.Target = target;
                context.Buff = removedBuff;
            });
        }
    }

    private bool RollDebuffResistance(BattleUnit target)
    {
        var resistance = Math.Clamp(target.GetStat(Game.Core.Model.StatType.AntiDebuff), 0d, 1d);
        return Probability.RollChance(random, resistance);
    }
}
