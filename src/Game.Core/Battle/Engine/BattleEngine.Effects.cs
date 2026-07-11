using Game.Core.Affix;
using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Core;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    private readonly record struct BattleSkillHitResolution(
        BattleUnit Target,
        bool IsHitConfirmed,
        int Damage,
        bool IsCritical,
        bool SuppressHitEffects);

    private readonly record struct BattleSkillHitCheck(bool IsCancelled, bool SuppressHitEffects);

    internal void ExecuteSkillDamageStep(
        BattleState state,
        BattleUnit source,
        IReadOnlyList<BattleUnit> targets,
        SkillInstance skill)
    {
        foreach (var target in targets)
        {
            var hit = ApplySkillDamage(state, source, target, skill);
            TryGainRageFromTakingDamage(state, source, hit.Target, hit.Damage);
            if (hit.IsHitConfirmed)
            {
                TriggerHooks(state, HookTiming.OnHitConfirmed, source, context =>
                {
                    context.Source = source;
                    context.Target = hit.Target;
                    context.Skill = skill;
                    context.DamageAmount = hit.Damage;
                    context.IsCritical = hit.IsCritical;
                });
            }

            if (!hit.SuppressHitEffects)
            {
                ApplySkillBuffs(state, source, hit.Target, skill.Buffs);
            }
        }

        if (targets.Any(target => state.AreEnemies(source, target)))
        {
            TryGainRageFromAttack(state, source);
        }
    }

    private BattleSkillHitResolution ApplySkillDamage(BattleState state, BattleUnit source, BattleUnit target, SkillInstance skill)
    {
        var hitCheck = ResolveSkillHit(state, source, target, skill);
        if (hitCheck.IsCancelled)
        {
            AddMessage(state, new BattleFact(
                BattleFactKind.Damaged,
                target.Id,
                detail: source.Id,
                damage: new BattleDamageEvent(0, SourceUnitId: source.Id)));
            return new BattleSkillHitResolution(target, false, 0, false, hitCheck.SuppressHitEffects);
        }

        var damageCalculation = _damageCalculator.CreateSkillDamageContext(
            new BattleDamageContext(source, target, skill, state.RuleSettings));
        ConfigureDamageCalculationHooks(state, source, target, skill, damageCalculation);
        var result = _damageCalculator.CalculateSkillDamage(damageCalculation);
        var damageMultiplier = BattleDamageRules.GetSkillDamageMultiplier(source, target);
        var resolvedDamageAmount = (int)Math.Floor(result.Amount * damageMultiplier);
        var damageApplication = _damageResolver.Apply(
            state,
            source,
            target,
            resolvedDamageAmount,
            skill,
            result.IsCritical);

        return new BattleSkillHitResolution(
            damageApplication.Target,
            true,
            damageApplication.ActualAmount,
            result.IsCritical,
            hitCheck.SuppressHitEffects || damageApplication.SuppressHitEffects);
    }

    private BattleSkillHitCheck ResolveSkillHit(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        SkillInstance skill)
    {
        var hitState = BattleHitState.Hit;
        var suppressHitEffects = false;

        if (Probability.RollChance(_random, target.GetStat(StatType.Evasion)))
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

        if (hitState == BattleHitState.Miss && Probability.RollChance(_random, source.GetStat(StatType.Accuracy)))
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

    internal void ApplySkillBuffs(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        IReadOnlyList<SkillBuffDefinition> buffs)
    {
        foreach (var buff in buffs)
        {
            if (!Probability.RollPercentage(_random, buff.Chance))
            {
                continue;
            }

            var buffTarget = buff.Buff.IsDebuff ? target : source;
            _battleBuffResolver.Apply(state, source, buffTarget, buff.Buff, buff.Level, buff.Duration, buff.Id);
        }
    }

    internal void ApplyHookExtraStrikeEffect(
        BattleHookContext context,
        BattleUnit target,
        ExtraStrikeBattleHookEffectDefinition effect)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(effect);

        if (context.DamageAmount is not > 0 || !target.IsAlive)
        {
            return;
        }

        var chance = Math.Clamp(
            (effect.Chance + effect.ChancePerBuffLevel * (context.Buff?.Level ?? 0)) / 100d,
            0d,
            1d);
        if (chance <= 0d)
        {
            return;
        }

        foreach (var factor in effect.DamageFactors)
        {
            if (!target.IsAlive)
            {
                return;
            }

            if (!Probability.RollChance(_random, chance))
            {
                continue;
            }

            var amount = (int)(context.DamageAmount.Value * factor);
            if (amount <= 0)
            {
                continue;
            }

            var damage = _damageResolver.Apply(
                context.State,
                context.Source ?? context.Unit,
                target,
                amount,
                context.Skill,
                context.IsCritical,
                eventTiming: context.Timing,
                detail: "extra_strike");
            if (damage.ActualAmount <= 0)
            {
                continue;
            }
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
                    _recoveryResolver.Apply(
                        state,
                        source,
                        target,
                        BattleRecoveryKind.Hp,
                        hp.Value);
                    break;
                case AddMpItemUseEffectDefinition mp:
                    _recoveryResolver.Apply(
                        state,
                        source,
                        target,
                        BattleRecoveryKind.Mp,
                        mp.Value);
                    break;
                case AddHpPercentItemUseEffectDefinition hpPercent:
                    _recoveryResolver.Apply(
                        state,
                        source,
                        target,
                        BattleRecoveryKind.Hp,
                        target.MaxHp * hpPercent.Value / 100);
                    break;
                case AddMpPercentItemUseEffectDefinition mpPercent:
                    _recoveryResolver.Apply(
                        state,
                        source,
                        target,
                        BattleRecoveryKind.Mp,
                        target.MaxMp * mpPercent.Value / 100);
                    break;
                case AddRageItemUseEffectDefinition rage:
                    BattleResourceResolver.AddRage(state, target, rage.Value, detailSource: "item");
                    break;
                case AddBuffItemUseEffectDefinition addBuff:
                {
                    var definition = _battleBuffResolver.Resolve(addBuff.BuffId);
                    _battleBuffResolver.Apply(
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
            _battleBuffResolver.NotifyRemoved(state, source, unit, [buff]);
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

        if (unit.HasTrait(TraitId.PoisonResistance))
        {
            damage /= 2;
        }

        if (damage <= 0)
        {
            return;
        }

        var source = state.TryGetUnit(buff.SourceUnitId) ?? unit;
        _damageResolver.Apply(
            state,
            source,
            unit,
            damage,
            runBeforeDamageApplied: false,
            detail: buff.Definition.Id);
    }

    private void ApplyRecoveryTick(BattleState state, BattleUnit unit, BattleBuffInstance buff)
    {
        var roll = 1d + _random.NextDouble() * 0.5d;
        var amount = Math.Max(1, (int)(unit.GetStat(StatType.Gengu) / 3d * buff.Level * roll));
        var source = state.TryGetUnit(buff.SourceUnitId) ?? unit;
        var restored = _recoveryResolver.Apply(
            state,
            source,
            unit,
            BattleRecoveryKind.Hp,
            amount).ActualAmount;
        AddMessage(state, new BattleFact(BattleFactKind.Healed, unit.Id, detail: $"{buff.Definition.Id}:{restored}"));
    }

    private void ApplyInternalInjuryTick(BattleState state, BattleUnit unit, BattleBuffInstance buff)
    {
        var baseValue = Math.Max(0d, (150d - unit.GetStat(StatType.Dingli)) / 4d);
        var roll = 1d + _random.NextDouble() * 0.5d;
        var amount = Math.Max(1, (int)(baseValue * buff.Level * roll));
        var drained = unit.DamageMp(amount);
        AddMessage(state, new BattleFact(BattleFactKind.MpDamaged, unit.Id, detail: $"{buff.Definition.Id}:{drained}"));
    }

    private void ApplyChargeTick(BattleState state, BattleUnit unit, BattleBuffInstance buff)
    {
        var chance = 0.15d + 0.2d * buff.Level;
        if (!Probability.RollChance(_random, chance))
        {
            return;
        }

        BattleResourceResolver.AddRage(state, unit, 1, detailSource: buff.Definition.Id);
    }

    private void TryGainRageFromAttack(BattleState state, BattleUnit unit)
    {
        if (!RollRageGain(unit))
        {
            return;
        }

        BattleResourceResolver.AddRage(state, unit, 1, detailSource: "attack");
    }

    private void TryGainRageFromTakingDamage(BattleState state, BattleUnit source, BattleUnit target, int damage)
    {
        if (damage <= 0 || !state.AreEnemies(source, target) || !RollRageGain(target))
        {
            return;
        }

        BattleResourceResolver.AddRage(state, target, 1, detailSource: "damaged");
    }

    private bool RollRageGain(BattleUnit unit)
    {
        var chance = 0.5d + unit.GetStat(StatType.Fuyuan) / 1000d;
        return Probability.RollChance(_random, chance);
    }
}
