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
        HookTiming? timing = null)
    {
        if (definition.Id == BattleContentIds.Poison && target.HasTrait(TraitId.PoisonImmunity))
        {
            state.AddMessage(new BattleFact(BattleFactKind.BuffResisted, target.Id, timing, detail: definition.Id));
            return false;
        }

        if (definition.IsDebuff && target.HasBuff(BattleContentIds.HolyWar))
        {
            return false;
        }

        if (definition.IsDebuff && RollDebuffResistance(target))
        {
            state.AddMessage(new BattleFact(BattleFactKind.BuffResisted, target.Id, timing, detail: definition.Id));
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

        if (!target.TryApplyBuff(instance))
        {
            return false;
        }

        state.AddMessage(new BattleFact(BattleFactKind.BuffApplied, target.Id, timing, detail: definition.Id));
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
        foreach (var removedBuff in removedBuffs)
        {
            NotifyRemoved(state, source, target, removedBuff, timing);
        }

        return removedBuffs;
    }

    public BattleBuffReductionEvent? Reduce(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        string buffId,
        int levelReduction,
        int durationReduction,
        HookTiming? timing = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buffId);
        ArgumentOutOfRangeException.ThrowIfNegative(levelReduction);
        ArgumentOutOfRangeException.ThrowIfNegative(durationReduction);
        if (levelReduction == 0 && durationReduction == 0)
        {
            throw new ArgumentException("At least one buff reduction must be positive.");
        }

        var buff = target.TryGetBuff(buffId);
        if (buff is null)
        {
            return null;
        }

        var remainingLevel = Math.Max(0, buff.Level - levelReduction);
        var remainingDuration = Math.Max(0, buff.RemainingTurns - durationReduction);
        var reduction = new BattleBuffReductionEvent(
            buff.Definition.Id,
            buff.Level - remainingLevel,
            buff.RemainingTurns - remainingDuration,
            remainingLevel,
            remainingDuration);

        if (remainingLevel == 0 || remainingDuration == 0)
        {
            if (!target.RemoveBuff(buff))
            {
                throw new InvalidOperationException($"Buff '{buff.Definition.Id}' is no longer attached to unit '{target.Id}'.");
            }

            NotifyRemoved(state, source, target, buff, timing);
            return reduction;
        }

        buff.Reduce(levelReduction, durationReduction);
        target.InvalidateLocalBattleProjection();
        target.ClampResourcesToLimits();
        state.AddMessage(new BattleFact(
            BattleFactKind.BuffReduced,
            target.Id,
            timing,
            detail: buff.Definition.Id,
            buffReduction: reduction));
        return reduction;
    }

    public void NotifyRemoved(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        BattleBuffInstance removedBuff,
        HookTiming? timing = null)
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

    private bool RollDebuffResistance(BattleUnit target)
    {
        var resistance = Math.Clamp(target.GetStat(Game.Core.Model.StatType.AntiDebuff), 0d, 1d);
        return Probability.RollChance(random, resistance);
    }
}
