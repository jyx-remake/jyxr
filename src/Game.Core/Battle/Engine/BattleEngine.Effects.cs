using Game.Core.Affix;
using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    private readonly record struct BattleSkillHitResolution(bool IsHitConfirmed, int Damage, bool SuppressHitEffects);

    private readonly record struct BattleSkillHitCheck(bool IsCancelled, bool SuppressHitEffects);

    private BattleSkillHitResolution ApplySkillDamage(BattleState state, BattleUnit source, BattleUnit target, SkillInstance skill)
    {
        var hitCheck = ResolveSkillHit(state, source, target, skill);
        if (hitCheck.IsCancelled)
        {
            AddEvent(state, new BattleEvent(
                BattleEventKind.Damaged,
                target.Id,
                Detail: source.Id,
                Damage: new BattleDamageEvent(0, SourceUnitId: source.Id)));
            return new BattleSkillHitResolution(false, 0, hitCheck.SuppressHitEffects);
        }

        var damageCalculation = _damageCalculator.CreateSkillDamageContext(new BattleDamageContext(source, target, skill));
        ConfigureDamageCalculationHooks(state, source, target, skill, damageCalculation);
        var result = _damageCalculator.CalculateSkillDamage(damageCalculation);
        var damageMultiplier = BattleDamageRules.GetSkillDamageMultiplier(source, target);
        var resolvedDamageAmount = (int)Math.Floor(result.Amount * damageMultiplier);
        var hookContext = TriggerHooks(state, HookTiming.OnDamageTaken, target, context =>
        {
            context.Source = source;
            context.Target = target;
            context.Skill = skill;
            context.DamageAmount = resolvedDamageAmount;
        });
        var damage = target.TakeDamage(Math.Max(0, hookContext.DamageAmount ?? resolvedDamageAmount));
        AddEvent(state, new BattleEvent(
            BattleEventKind.Damaged,
            target.Id,
            Detail: source.Id,
            Damage: new BattleDamageEvent(damage, result.IsCritical, source.Id)));
        return new BattleSkillHitResolution(true, damage, hitCheck.SuppressHitEffects || hookContext.SuppressHitEffects);
    }

    private BattleSkillHitCheck ResolveSkillHit(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        SkillInstance skill)
    {
        var hitState = BattleHitState.Hit;
        var suppressHitEffects = false;

        if (_random.RollChance(target.GetStat(StatType.Evasion)))
        {
            hitState = BattleHitState.Miss;
            suppressHitEffects = true;
        }

        void Configure(BattleHookContext context)
        {
            context.Source = source;
            context.Target = target;
            context.Skill = skill;
            context.HitState = hitState;
            context.SuppressHitEffects = suppressHitEffects;
        }

        void Apply(BattleHookContext context)
        {
            hitState = context.HitState;
            suppressHitEffects = context.SuppressHitEffects;
        }

        Apply(TriggerHooks(state, HookTiming.BeforeHitResolved, target, Configure));
        if (!string.Equals(source.Id, target.Id, StringComparison.Ordinal))
        {
            Apply(TriggerHooks(
                state,
                HookTiming.BeforeHitResolved,
                source,
                Configure,
                hookFilter: static hook => hook.Conditions
                    .OfType<ContextHitStateBattleHookConditionDefinition>()
                    .Any(condition => condition.State == BattleHitState.Miss)));
            Apply(TriggerHooks(
                state,
                HookTiming.BeforeHitResolved,
                source,
                Configure,
                hookFilter: static hook => !hook.Conditions
                    .OfType<ContextHitStateBattleHookConditionDefinition>()
                    .Any()));
        }

        if (hitState == BattleHitState.Miss && _random.RollChance(source.GetStat(StatType.Accuracy)))
        {
            hitState = BattleHitState.Hit;
            suppressHitEffects = false;
        }

        return new BattleSkillHitCheck(hitState == BattleHitState.Miss, suppressHitEffects);
    }

    private void ConfigureDamageCalculationHooks(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        SkillInstance skill,
        BattleDamageCalculationContext damageCalculation)
    {
        void Configure(BattleHookContext context)
        {
            context.Source = source;
            context.Target = target;
            context.Skill = skill;
            context.DamageCalculation = damageCalculation;
        }

        TriggerHooks(state, HookTiming.BeforeDamageCalculation, source, Configure);
        if (!string.Equals(source.Id, target.Id, StringComparison.Ordinal))
        {
            TriggerHooks(state, HookTiming.BeforeDamageCalculation, target, Configure);
        }
    }

    private void ApplySkillBuffs(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        IReadOnlyList<SkillBuffDefinition> buffs)
    {
        foreach (var buff in buffs)
        {
            if (!_random.RollPercentage(buff.Chance))
            {
                continue;
            }

            var buffTarget = buff.Buff.IsDebuff ? target : source;
            ApplyBattleBuff(state, source, buffTarget, buff.Buff, buff.Level, buff.Duration, buff.Id);
        }
    }

    private bool ApplyBattleBuff(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        BuffDefinition buffDefinition,
        int level,
        int duration,
        string detail)
    {
        var instance = new BattleBuffInstance(
            buffDefinition,
            level,
            duration,
            source.Id,
            state.ActionSerial);
        var hookContext = TriggerHooks(state, HookTiming.BeforeBuffApplied, source, context =>
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
        AddEvent(state, new BattleEvent(BattleEventKind.BuffApplied, target.Id, Detail: detail));
        TriggerHooks(state, HookTiming.OnBuffApplied, target, context =>
        {
            context.Source = source;
            context.Target = target;
            context.Buff = instance;
        });
        return true;
    }

    internal bool ApplyHookBuffEffect(
        BattleHookContext context,
        BattleUnit target,
        ApplyBuffBattleEffectDefinition effect)
    {
        var source = context.Source ?? context.Unit;
        var buffDefinition = effect.Buff ?? _buffResolver(effect.BuffId);
        return ApplyBattleBuff(
            context.State,
            source,
            target,
            buffDefinition,
            effect.Level,
            effect.Duration,
            $"{context.Timing}:{effect.BuffId}");
    }

    internal IReadOnlyList<BattleBuffInstance> RemoveHookBuffById(
        BattleHookContext context,
        BattleUnit target,
        string buffId,
        string? detailPrefix)
    {
        var source = context.Source ?? context.Unit;
        return RemoveBattleBuffs(
            context.State,
            source,
            target,
            buff => string.Equals(buff.Definition.Id, buffId, StringComparison.Ordinal),
            detailPrefix);
    }

    internal IReadOnlyList<BattleBuffInstance> RemoveHookNegativeBuffs(
        BattleHookContext context,
        BattleUnit target,
        string? detailPrefix)
    {
        var source = context.Source ?? context.Unit;
        return RemoveBattleBuffs(
            context.State,
            source,
            target,
            buff => buff.Definition.IsDebuff,
            detailPrefix);
    }

    internal IReadOnlyList<BattleBuffInstance> RemoveHookPositiveBuffs(
        BattleHookContext context,
        BattleUnit target,
        string? detailPrefix)
    {
        var source = context.Source ?? context.Unit;
        return RemoveBattleBuffs(
            context.State,
            source,
            target,
            buff => !buff.Definition.IsDebuff,
            detailPrefix);
    }

    private void ApplySpecialSkillEffects(
        BattleState state,
        BattleUnit source,
        IReadOnlyList<BattleUnit> targets,
        IReadOnlyList<BattleEffectDefinition>? effects)
    {
        if (effects is null || effects.Count == 0)
        {
            return;
        }

        foreach (var effect in effects)
        {
            foreach (var target in SelectSpecialSkillEffectTargets(state, source, targets, effect))
            {
                ApplySpecialSkillEffect(state, source, target, effect);
            }
        }
    }

    private void ApplySpecialSkillEffect(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        BattleEffectDefinition effect)
    {
        switch (effect)
        {
            case RemoveBuffBattleEffectDefinition removeBuff:
                RemoveBattleBuffs(
                    state,
                    source,
                    target,
                    buff => string.Equals(buff.Definition.Id, removeBuff.BuffId, StringComparison.Ordinal),
                    "special_skill");
                break;
            case RemoveNegativeBuffsBattleEffectDefinition:
                RemoveBattleBuffs(state, source, target, buff => buff.Definition.IsDebuff, "special_skill");
                break;
            case RemovePositiveBuffsBattleEffectDefinition:
                RemoveBattleBuffs(state, source, target, buff => !buff.Definition.IsDebuff, "special_skill");
                break;
            case AddRageBattleEffectDefinition rage:
                target.AddRage(rage.Value);
                AddEvent(state, new BattleEvent(BattleEventKind.RageChanged, target.Id, Detail: $"special_skill:{rage.Value}"));
                break;
            case SetRageBattleEffectDefinition rage:
                target.SetRage(rage.Value);
                AddEvent(state, new BattleEvent(BattleEventKind.RageChanged, target.Id, Detail: $"special_skill:set:{rage.Value}"));
                break;
            case SetActionGaugeBattleEffectDefinition gauge:
                target.SetActionGauge(gauge.Value);
                break;
            case AddHpBattleEffectDefinition hp:
            {
                var restored = target.RestoreHp(hp.Value);
                AddEvent(state, new BattleEvent(BattleEventKind.Healed, target.Id, Detail: $"special_skill:{restored}"));
                break;
            }
            case AddMpBattleEffectDefinition mp:
                target.RestoreMp(mp.Value);
                break;
            case ApplyBuffBattleEffectDefinition addBuff:
            {
                var buffDefinition = addBuff.Buff ?? _buffResolver(addBuff.BuffId);
                if (!_random.RollPercentage(addBuff.Chance))
                {
                    break;
                }

                ApplyBattleBuff(
                    state,
                    source,
                    target,
                    buffDefinition,
                    addBuff.Level,
                    addBuff.Duration,
                    addBuff.BuffId);
                break;
            }
            default:
                throw new NotSupportedException($"Unsupported special skill effect '{effect.GetType().Name}'.");
        }
    }

    private static IReadOnlyList<BattleUnit> SelectSpecialSkillEffectTargets(
        BattleState state,
        BattleUnit source,
        IReadOnlyList<BattleUnit> impactedTargets,
        BattleEffectDefinition effect) =>
        effect switch
        {
            ApplyBuffBattleEffectDefinition applyBuff => SelectSpecialSkillEffectTargets(state, source, impactedTargets, applyBuff.Target),
            RemoveBuffBattleEffectDefinition removeBuff => SelectSpecialSkillEffectTargets(state, source, impactedTargets, removeBuff.Target),
            RemoveNegativeBuffsBattleEffectDefinition removeNegativeBuffs => SelectSpecialSkillEffectTargets(state, source, impactedTargets, removeNegativeBuffs.Target),
            RemovePositiveBuffsBattleEffectDefinition removePositiveBuffs => SelectSpecialSkillEffectTargets(state, source, impactedTargets, removePositiveBuffs.Target),
            AddRageBattleEffectDefinition addRage => SelectSpecialSkillEffectTargets(state, source, impactedTargets, addRage.Target),
            SetRageBattleEffectDefinition setRage => SelectSpecialSkillEffectTargets(state, source, impactedTargets, setRage.Target),
            SetActionGaugeBattleEffectDefinition setActionGauge => SelectSpecialSkillEffectTargets(state, source, impactedTargets, setActionGauge.Target),
            AddHpBattleEffectDefinition addHp => SelectSpecialSkillEffectTargets(state, source, impactedTargets, addHp.Target),
            AddMpBattleEffectDefinition addMp => SelectSpecialSkillEffectTargets(state, source, impactedTargets, addMp.Target),
            _ => throw new NotSupportedException($"Unsupported special skill effect '{effect.GetType().Name}'.")
        };

    private static IReadOnlyList<BattleUnit> SelectSpecialSkillEffectTargets(
        BattleState state,
        BattleUnit source,
        IReadOnlyList<BattleUnit> impactedTargets,
        BattleTargetSelectorDefinition selector) =>
        selector switch
        {
            SelfBattleTargetSelectorDefinition => [source],
            SourceBattleTargetSelectorDefinition => [source],
            TargetBattleTargetSelectorDefinition => impactedTargets,
            AllAlliesBattleTargetSelectorDefinition allAllies => state.GetLivingUnits()
                .Where(unit => unit.Team == source.Team)
                .Where(unit => allAllies.IncludeSelf || !string.Equals(unit.Id, source.Id, StringComparison.Ordinal))
                .ToList(),
            AllEnemiesBattleTargetSelectorDefinition => state.GetLivingUnits()
                .Where(unit => unit.Team != source.Team)
                .ToList(),
            NearbyAlliesBattleTargetSelectorDefinition nearbyAllies => state.GetLivingUnits()
                .Where(unit => unit.Team == source.Team)
                .Where(unit => nearbyAllies.IncludeSelf || !string.Equals(unit.Id, source.Id, StringComparison.Ordinal))
                .Where(unit => unit.Position.ManhattanDistanceTo(source.Position) <= nearbyAllies.Radius)
                .ToList(),
            _ => throw new NotSupportedException($"Unsupported battle target selector '{selector.GetType().Name}'.")
        };

    private IReadOnlyList<BattleBuffInstance> RemoveBattleBuffs(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        Func<BattleBuffInstance, bool> predicate,
        string? detailPrefix = null)
    {
        var removedBuffs = target.RemoveBuffs(predicate);
        EmitBuffRemovedEvents(state, source, target, removedBuffs, detailPrefix);
        return removedBuffs;
    }

    private void EmitBuffRemovedEvents(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        IReadOnlyList<BattleBuffInstance> removedBuffs,
        string? detailPrefix = null)
    {
        foreach (var removedBuff in removedBuffs)
        {
            var detail = string.IsNullOrWhiteSpace(detailPrefix)
                ? removedBuff.Definition.Id
                : $"{detailPrefix}:{removedBuff.Definition.Id}";
            AddEvent(state, new BattleEvent(BattleEventKind.BuffRemoved, target.Id, Detail: detail));
            TriggerHooks(state, HookTiming.OnBuffRemoved, target, context =>
            {
                context.Source = source;
                context.Target = target;
                context.Buff = removedBuff;
            });
        }
    }

    private void ApplyItemEffects(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        IReadOnlyList<ItemUseEffectDefinition> effects)
    {
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case AddHpItemUseEffectDefinition hp:
                    target.RestoreHp(hp.Value);
                    break;
                case AddMpItemUseEffectDefinition mp:
                    target.RestoreMp(mp.Value);
                    break;
                case AddHpPercentItemUseEffectDefinition hpPercent:
                    target.RestoreHp(target.MaxHp * hpPercent.Value / 100);
                    break;
                case AddMpPercentItemUseEffectDefinition mpPercent:
                    target.RestoreMp(target.MaxMp * mpPercent.Value / 100);
                    break;
                case AddRageItemUseEffectDefinition rage:
                    target.AddRage(rage.Value);
                    break;
                case AddBuffItemUseEffectDefinition addBuff:
                {
                    var definition = _buffResolver(addBuff.BuffId);
                    ApplyBattleBuff(
                        state,
                        source,
                        target,
                        definition,
                        addBuff.Level,
                        addBuff.Duration,
                        addBuff.BuffId);
                    break;
                }
            }
        }
    }

    private void AdvanceBuffTimelines(BattleState state, BattleUnit unit)
    {
        foreach (var buff in unit.Buffs.Where(static buff => !buff.IsExpired).ToArray())
        {
            if (!buff.AdvanceTimeline())
            {
                continue;
            }

            ApplyPeriodicBuffEffect(state, unit, buff);
            buff.ConsumeRound();
        }

        var expired = unit.RemoveExpiredBuffs();
        foreach (var buff in expired)
        {
            var source = state.TryGetUnit(buff.SourceUnitId) ?? unit;
            EmitBuffRemovedEvents(state, source, unit, [buff]);
        }
    }

    private void ApplyPeriodicBuffEffect(BattleState state, BattleUnit unit, BattleBuffInstance buff)
    {
        switch (buff.Definition.Id)
        {
            case BattleContentIds.Poison:
                ApplyPoisonTick(state, unit, buff);
                break;
            case BattleContentIds.Recovery:
                ApplyRecoveryTick(state, unit, buff);
                break;
            case BattleContentIds.InternalInjury:
                ApplyInternalInjuryTick(state, unit, buff);
                break;
            case BattleContentIds.Charge:
                ApplyChargeTick(state, unit, buff);
                break;
        }
    }

    private void ApplyPoisonTick(BattleState state, BattleUnit unit, BattleBuffInstance buff)
    {
        var dingli = Math.Min(unit.GetStat(StatType.Dingli), 200d);
        var resistanceFactor = 1d - dingli / 200d * 0.5d;
        var roll = 0.5d + _random.NextDouble() * 0.5d;
        var damage = Math.Max(1, (int)(35d * buff.Level * resistanceFactor * roll));
        if (unit.Hp - damage < 1)
        {
            damage = Math.Max(0, unit.Hp - 1);
        }

        if (unit.Character.HasEffectiveTalent(BattleContentIds.PoisonResistance))
        {
            damage /= 2;
        }

        if (damage <= 0)
        {
            return;
        }

        unit.TakeDamage(damage);
        AddEvent(state, new BattleEvent(
            BattleEventKind.Damaged,
            unit.Id,
            Detail: buff.Definition.Id,
            Damage: new BattleDamageEvent(damage, SourceUnitId: buff.SourceUnitId)));
    }

    private void ApplyRecoveryTick(BattleState state, BattleUnit unit, BattleBuffInstance buff)
    {
        var roll = 1d + _random.NextDouble() * 0.5d;
        var amount = Math.Max(1, (int)(unit.GetStat(StatType.Gengu) / 3d * buff.Level * roll));
        var restored = unit.RestoreHp(amount);
        AddEvent(state, new BattleEvent(BattleEventKind.Healed, unit.Id, Detail: $"{buff.Definition.Id}:{restored}"));
    }

    private void ApplyInternalInjuryTick(BattleState state, BattleUnit unit, BattleBuffInstance buff)
    {
        var baseValue = Math.Max(0d, (150d - unit.GetStat(StatType.Dingli)) / 4d);
        var roll = 1d + _random.NextDouble() * 0.5d;
        var amount = Math.Max(1, (int)(baseValue * buff.Level * roll));
        var drained = unit.DamageMp(amount);
        AddEvent(state, new BattleEvent(BattleEventKind.MpDamaged, unit.Id, Detail: $"{buff.Definition.Id}:{drained}"));
    }

    private void ApplyChargeTick(BattleState state, BattleUnit unit, BattleBuffInstance buff)
    {
        var chance = 0.15d + 0.2d * buff.Level;
        if (!_random.RollChance(chance))
        {
            return;
        }

        unit.AddRage(1);
        AddEvent(state, new BattleEvent(BattleEventKind.RageChanged, unit.Id, Detail: $"{buff.Definition.Id}:1"));
    }

    private void TryGainRageFromAttack(BattleState state, BattleUnit unit)
    {
        if (!RollRageGain(unit))
        {
            return;
        }

        unit.AddRage(1);
        AddEvent(state, new BattleEvent(BattleEventKind.RageChanged, unit.Id, Detail: "attack:1"));
    }

    private void TryGainRageFromTakingDamage(BattleState state, BattleUnit source, BattleUnit target, int damage)
    {
        if (damage <= 0 || !state.AreEnemies(source, target) || !RollRageGain(target))
        {
            return;
        }

        target.AddRage(1);
        AddEvent(state, new BattleEvent(BattleEventKind.RageChanged, target.Id, Detail: "damaged:1"));
    }

    private bool RollRageGain(BattleUnit unit)
    {
        var chance = 0.5d + unit.GetStat(StatType.Fuyuan) / 1000d;
        return _random.RollChance(chance);
    }
}
