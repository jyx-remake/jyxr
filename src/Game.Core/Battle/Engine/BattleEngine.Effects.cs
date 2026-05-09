using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed partial class BattleEngine
{
    private int ApplySkillDamage(BattleState state, BattleUnit source, BattleUnit target, SkillInstance skill)
    {
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
            Detail: $"{source.Id}:{damage}"));
        return damage;
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
            if (buff.Chance <= 0)
            {
                continue;
            }

            var instance = new BattleBuffInstance(
                buff.Buff,
                buff.Level,
                buff.Duration,
                source.Id,
                state.ActionSerial);
            var buffTarget = buff.Buff.IsDebuff ? target : source;
            var hookContext = TriggerHooks(state, HookTiming.BeforeBuffApplied, source, context =>
            {
                context.Source = source;
                context.Target = buffTarget;
                context.Buff = instance;
            });
            if (hookContext.Cancel)
            {
                continue;
            }

            buffTarget.ApplyBuff(instance);

            var battleEvent = new BattleEvent(BattleEventKind.BuffApplied, buffTarget.Id, Detail: buff.Id);
            AddEvent(state, battleEvent);
            TriggerHooks(state, HookTiming.OnBuffApplied, buffTarget);
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
                    var instance = new BattleBuffInstance(definition, addBuff.Level, addBuff.Duration, source.Id, state.ActionSerial);
                    var hookContext = TriggerHooks(state, HookTiming.BeforeBuffApplied, source, context =>
                    {
                        context.Source = source;
                        context.Target = target;
                        context.Buff = instance;
                    });
                    if (hookContext.Cancel)
                    {
                        break;
                    }

                    target.ApplyBuff(instance);
                    AddEvent(state, new BattleEvent(BattleEventKind.BuffApplied, target.Id, Detail: addBuff.BuffId));
                    TriggerHooks(state, HookTiming.OnBuffApplied, target);
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
            AddEvent(state, new BattleEvent(BattleEventKind.BuffExpired, unit.Id, Detail: buff.Definition.Id));
            TriggerHooks(state, HookTiming.OnBuffExpired, unit);
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
        AddEvent(state, new BattleEvent(BattleEventKind.Damaged, unit.Id, Detail: $"{buff.Definition.Id}:{damage}"));
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
        if (_random.NextDouble() > chance)
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
        return _random.NextDouble() < Math.Clamp(chance, 0d, 1d);
    }
}
