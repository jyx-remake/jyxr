using Game.Core.Affix;
using Game.Core.Abstractions;
using Game.Core;

namespace Game.Core.Battle;

public sealed class BattleHookExecutor
{
    public void Execute(BattleHookContext context, HookAffix hook)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(hook);

        if (hook.Timing != context.Timing)
        {
            throw new InvalidOperationException($"Hook timing '{hook.Timing}' does not match context timing '{context.Timing}'.");
        }

        EnsurePreviewSafe(context, hook);

        if (hook.Conditions.Any(condition => !EvaluateCondition(context, condition)))
        {
            return;
        }

        foreach (var effect in hook.Effects)
        {
            ApplyEffect(context, effect);
        }

        TryRequestSpeech(context, hook.Speech);
    }

    private static bool EvaluateCondition(BattleHookContext context, BattleHookConditionDefinition condition) =>
        condition switch
        {
            ChanceBattleHookConditionDefinition chance => Probability.RollChance(context.Random, chance.Value),
            DamagePositiveBattleHookConditionDefinition => context.DamageAmount is > 0,
            ContextBuffIdBattleHookConditionDefinition buffId => context.Buff is not null &&
                string.Equals(context.Buff.Definition.Id, buffId.BuffId, StringComparison.Ordinal),
            ContextUnitHpRatioBattleHookConditionDefinition hpRatio => IsContextUnitHpRatio(context, hpRatio),
            ContextUnitEffectiveTalentBattleHookConditionDefinition talent =>
                talent.TalentIds.Any(context.Unit.Character.HasEffectiveTalent),
            ContextUnitEquippedInternalSkillBattleHookConditionDefinition internalSkill =>
                internalSkill.InternalSkillIds.Any(id => string.Equals(id, GetEquippedInternalSkillId(context.Unit), StringComparison.Ordinal)),
            ContextUnitRelationBattleHookConditionDefinition relation =>
                IsContextUnitRelation(context, relation),
            ContextUnitRoleBattleHookConditionDefinition unitRole => IsContextUnitRole(context, unitRole.Role),
            ContextUnitGenderBattleHookConditionDefinition gender => IsContextUnitGender(context, gender),
            ContextHitStateBattleHookConditionDefinition hitState => context.HitState == hitState.State,
            ContextSkillNameEqualsBattleHookConditionDefinition skillName =>
                context.Skill is not null &&
                skillName.Values.Any(value => string.Equals(context.Skill.Name, value, StringComparison.Ordinal)),
            ContextSkillNameContainsBattleHookConditionDefinition skillName =>
                context.Skill is not null &&
                skillName.Values.Any(value => context.Skill.Name.Contains(value, StringComparison.Ordinal)),
            ContextSkillWeaponTypeBattleHookConditionDefinition skillWeaponType =>
                context.Skill is not null &&
                skillWeaponType.WeaponTypes.Contains(context.Skill.WeaponType),
            _ => throw new NotSupportedException($"Unsupported battle hook condition '{condition.GetType().Name}'.")
        };

    private static void EnsurePreviewSafe(BattleHookContext context, HookAffix hook)
    {
        if (!context.IsPreview)
        {
            return;
        }

        if (hook.Speech is not null)
        {
            throw new InvalidOperationException(
                $"Preview battle hook execution does not support speech on timing '{hook.Timing}'.");
        }

        foreach (var condition in hook.Conditions)
        {
            if (condition is ChanceBattleHookConditionDefinition)
            {
                throw new InvalidOperationException(
                    $"Preview battle hook execution does not support random chance conditions on timing '{hook.Timing}'.");
            }
        }

        foreach (var effect in hook.Effects)
        {
            switch (effect)
            {
                case ModifyDamageBattleHookEffectDefinition:
                case ModifyDamageContextBattleHookEffectDefinition:
                case ModifyMpCostBattleHookEffectDefinition:
                    continue;
                case StrengthenContextBuffBattleHookEffectDefinition:
                case ApplyBuffBattleEffectDefinition:
                case RemoveBuffBattleEffectDefinition:
                case RemoveNegativeBuffsBattleEffectDefinition:
                case RemovePositiveBuffsBattleEffectDefinition:
                case AddRageBattleEffectDefinition:
                case SetRageBattleEffectDefinition:
                case SetActionGaugeBattleEffectDefinition:
                case AddHpBattleEffectDefinition:
                case AddMpBattleEffectDefinition:
                case CancelHitBattleHookEffectDefinition:
                case SetHitSuccessBattleHookEffectDefinition:
                    throw new InvalidOperationException(
                        $"Preview battle hook execution does not support side-effect effect '{effect.GetType().Name}' on timing '{hook.Timing}'.");
                default:
                    throw new NotSupportedException(
                        $"Unsupported preview battle hook effect '{effect.GetType().Name}'.");
            }
        }
    }

    private static void ApplyEffect(BattleHookContext context, BattleEffectDefinition effect)
    {
        switch (effect)
        {
            case ModifyDamageBattleHookEffectDefinition modifyDamage:
                context.DamageAmount = ApplyModifier(context.DamageAmount, context, modifyDamage.Op, modifyDamage.Delta, modifyDamage.DeltaPerBuffLevel, modifyDamage.Rounding);
                break;

            case ModifyDamageContextBattleHookEffectDefinition modifyDamageContext:
                ApplyDamageContextModifier(context, modifyDamageContext);
                break;

            case ModifyMpCostBattleHookEffectDefinition modifyMpCost:
                context.MpCost = ApplyModifier(context.MpCost, context, modifyMpCost.Op, modifyMpCost.Delta, modifyMpCost.DeltaPerBuffLevel, modifyMpCost.Rounding);
                break;

            case StrengthenContextBuffBattleHookEffectDefinition strengthenBuff:
                var buff = context.Buff ?? throw new InvalidOperationException("Battle hook effect requires a context buff.");
                buff.Strengthen(strengthenBuff.LevelDelta, strengthenBuff.TurnDelta);
                break;

            case ApplyBuffBattleEffectDefinition applyBuff:
                ApplyToSelectedTargets(context, applyBuff.Target, target =>
                {
                    if (Probability.RollPercentage(context.Random, applyBuff.Chance))
                    {
                        context.Engine.ApplyHookBuffEffect(context, target, applyBuff);
                    }
                });
                break;

            case RemoveBuffBattleEffectDefinition removeBuff:
                ApplyToSelectedTargets(context, removeBuff.Target, target =>
                {
                    context.Engine.RemoveHookBuffById(
                        context,
                        target,
                        removeBuff.BuffId,
                        context.Timing.ToString());
                });
                break;

            case RemoveNegativeBuffsBattleEffectDefinition removeNegativeBuffs:
                ApplyToSelectedTargets(context, removeNegativeBuffs.Target, target =>
                {
                    context.Engine.RemoveHookNegativeBuffs(context, target, context.Timing.ToString());
                });
                break;

            case RemovePositiveBuffsBattleEffectDefinition removePositiveBuffs:
                ApplyToSelectedTargets(context, removePositiveBuffs.Target, target =>
                {
                    context.Engine.RemoveHookPositiveBuffs(context, target, context.Timing.ToString());
                });
                break;

            case AddRageBattleEffectDefinition addRage:
                ApplyToSelectedTargets(context, addRage.Target, target =>
                {
                    target.AddRage(addRage.Value);
                    context.State.AddEvent(new BattleEvent(BattleEventKind.RageChanged, target.Id, Detail: $"{context.Timing}:{addRage.Value}"));
                });
                break;

            case SetRageBattleEffectDefinition setRage:
                ApplyToSelectedTargets(context, setRage.Target, target =>
                {
                    target.SetRage(setRage.Value);
                    context.State.AddEvent(new BattleEvent(BattleEventKind.RageChanged, target.Id, Detail: $"{context.Timing}:set:{setRage.Value}"));
                });
                break;

            case SetActionGaugeBattleEffectDefinition setActionGauge:
                ApplyToSelectedTargets(context, setActionGauge.Target, target =>
                {
                    target.SetActionGauge(setActionGauge.Value);
                });
                break;

            case AddHpBattleEffectDefinition addHp:
                ApplyToSelectedTargets(context, addHp.Target, target =>
                {
                    var restored = target.RestoreHp(addHp.Value);
                    context.State.AddEvent(new BattleEvent(BattleEventKind.Healed, target.Id, Detail: $"{context.Timing}:{restored}"));
                });
                break;

            case AddMpBattleEffectDefinition addMp:
                ApplyToSelectedTargets(context, addMp.Target, target =>
                {
                    target.RestoreMp(addMp.Value);
                });
                break;

            case CancelHitBattleHookEffectDefinition cancelHit:
                context.HitState = BattleHitState.Miss;
                context.DamageAmount = 0;
                context.SuppressHitEffects = cancelHit.SuppressHitEffects;
                break;

            case SetHitSuccessBattleHookEffectDefinition:
                context.HitState = BattleHitState.Hit;
                context.SuppressHitEffects = false;
                break;

            default:
                throw new NotSupportedException($"Unsupported battle hook effect '{effect.GetType().Name}'.");
        }
    }

    private static void ApplyDamageContextModifier(
        BattleHookContext context,
        ModifyDamageContextBattleHookEffectDefinition effect)
    {
        var damageCalculation = context.DamageCalculation
            ?? throw new InvalidOperationException("Battle hook effect requires a damage calculation context.");
        var delta = ResolveDamageContextDelta(context, effect)
            + effect.DeltaPerUnitLevel * context.Unit.Character.Level
            + effect.DeltaPerBuffLevel * (context.Buff?.Level ?? 0);
        damageCalculation.AddModifier(effect.Field, effect.Op, delta);
    }

    private static double ResolveDamageContextDelta(
        BattleHookContext context,
        ModifyDamageContextBattleHookEffectDefinition effect)
    {
        if (effect.DeltaMin is null || effect.DeltaMax is null)
        {
            return effect.Delta;
        }

        if (context.IsPreview)
        {
            return (effect.DeltaMin.Value + effect.DeltaMax.Value) / 2d;
        }

        var min = effect.DeltaMin.Value;
        var max = effect.DeltaMax.Value;
        return min + (max - min) * context.Random.NextDouble();
    }

    private static int? ApplyModifier(
        int? currentValue,
        BattleHookContext context,
        ModifierOp op,
        double delta,
        double deltaPerBuffLevel,
        BattleHookRounding rounding)
    {
        if (currentValue is null)
        {
            return null;
        }

        var resolvedDelta = delta + deltaPerBuffLevel * (context.Buff?.Level ?? 0);
        var value = op switch
        {
            ModifierOp.Add => currentValue.Value + resolvedDelta,
            ModifierOp.Increase => currentValue.Value * (1d + resolvedDelta),
            ModifierOp.More => currentValue.Value * resolvedDelta,
            ModifierOp.PostAdd => currentValue.Value + resolvedDelta,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };

        return Math.Max(0, Round(value, rounding));
    }

    private static int Round(double value, BattleHookRounding rounding) =>
        rounding switch
        {
            BattleHookRounding.Truncate => (int)value,
            BattleHookRounding.Floor => (int)Math.Floor(value),
            BattleHookRounding.Ceiling => (int)Math.Ceiling(value),
            BattleHookRounding.Round => (int)Math.Round(value, MidpointRounding.AwayFromZero),
            _ => throw new ArgumentOutOfRangeException(nameof(rounding), rounding, null)
        };

    private static void ApplyToSelectedTargets(
        BattleHookContext context,
        BattleTargetSelectorDefinition selector,
        Action<BattleUnit> apply)
    {
        foreach (var target in SelectTargets(context, selector))
        {
            apply(target);
        }
    }

    private static IReadOnlyList<BattleUnit> SelectTargets(BattleHookContext context, BattleTargetSelectorDefinition selector) =>
        selector switch
        {
            SelfBattleTargetSelectorDefinition => [context.Unit],
            SourceBattleTargetSelectorDefinition => context.Source is null ? [] : [context.Source],
            TargetBattleTargetSelectorDefinition => context.Target is null ? [] : [context.Target],
            AllAlliesBattleTargetSelectorDefinition allAllies => context.State.GetLivingUnits()
                .Where(unit => unit.Team == context.Unit.Team)
                .Where(unit => allAllies.IncludeSelf || !string.Equals(unit.Id, context.Unit.Id, StringComparison.Ordinal))
                .ToList(),
            AllEnemiesBattleTargetSelectorDefinition => context.State.GetLivingUnits()
                .Where(unit => unit.Team != context.Unit.Team)
                .ToList(),
            NearbyAlliesBattleTargetSelectorDefinition nearbyAllies => context.State.GetLivingUnits()
                .Where(unit => unit.Team == context.Unit.Team)
                .Where(unit => nearbyAllies.IncludeSelf || !string.Equals(unit.Id, context.Unit.Id, StringComparison.Ordinal))
                .Where(unit => unit.Position.ManhattanDistanceTo(context.Unit.Position) <= nearbyAllies.Radius)
                .ToList(),
            _ => throw new NotSupportedException($"Unsupported battle target selector '{selector.GetType().Name}'.")
        };

    private static void TryRequestSpeech(
        BattleHookContext context,
        BattleSpeechDefinition? speech)
    {
        if (speech is null)
        {
            return;
        }

        var speaker = ResolveSpeaker(context, speech.Speaker);
        var line = BattleSpeechRuntime.TryPickLine(speech, context.Random);
        line = BattleSpeechRuntime.FormatText(line, context.Unit, context.Source, context.Target);
        BattleSpeechRuntime.TryEmit(context.State, speaker, line, context.Timing);
    }

    private static BattleUnit? ResolveSpeaker(BattleHookContext context, BattleSpeechSpeaker speaker) =>
        speaker switch
        {
            BattleSpeechSpeaker.Owner => context.Unit,
            BattleSpeechSpeaker.Source => context.Source,
            BattleSpeechSpeaker.Target => context.Target,
            _ => throw new ArgumentOutOfRangeException(nameof(speaker), speaker, null)
        };

    private static string? GetEquippedInternalSkillId(BattleUnit unit) =>
        unit.Character.GetInternalSkills()
            .FirstOrDefault(static skill => skill.IsEquipped)
            ?.Definition.Id;

    private static bool IsContextUnitRole(BattleHookContext context, BattleHookContextUnitRole role) =>
        role switch
        {
            BattleHookContextUnitRole.Source => context.Source is not null &&
                string.Equals(context.Unit.Id, context.Source.Id, StringComparison.Ordinal),
            BattleHookContextUnitRole.Target => context.Target is not null &&
                string.Equals(context.Unit.Id, context.Target.Id, StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

    private static bool IsContextUnitRelation(
        BattleHookContext context,
        ContextUnitRelationBattleHookConditionDefinition condition)
    {
        var otherUnit = condition.Role switch
        {
            BattleHookContextUnitRole.Source => context.Source,
            BattleHookContextUnitRole.Target => context.Target,
            _ => throw new ArgumentOutOfRangeException(nameof(condition.Role), condition.Role, null)
        };

        if (otherUnit is null)
        {
            return false;
        }

        var areAllies = context.Unit.Team == otherUnit.Team;
        return condition.Relation switch
        {
            BattleHookRelation.Ally => areAllies,
            BattleHookRelation.Enemy => !areAllies,
            _ => throw new ArgumentOutOfRangeException(nameof(condition.Relation), condition.Relation, null)
        };
    }

    private static bool IsContextUnitGender(
        BattleHookContext context,
        ContextUnitGenderBattleHookConditionDefinition condition)
    {
        var unit = condition.Role switch
        {
            BattleHookContextUnitRole.Source => context.Source,
            BattleHookContextUnitRole.Target => context.Target,
            _ => throw new ArgumentOutOfRangeException(nameof(condition.Role), condition.Role, null)
        };

        return unit is not null && condition.Genders.Contains(unit.Character.Definition.Gender);
    }

    private static bool IsContextUnitHpRatio(
        BattleHookContext context,
        ContextUnitHpRatioBattleHookConditionDefinition condition)
    {
        var ratio = context.Unit.MaxHp <= 0
            ? 0d
            : (double)context.Unit.Hp / context.Unit.MaxHp;
        if (condition.MinInclusive is { } minInclusive && ratio < minInclusive)
        {
            return false;
        }

        if (condition.MinExclusive is { } minExclusive && ratio <= minExclusive)
        {
            return false;
        }

        if (condition.MaxExclusive is { } maxExclusive && ratio >= maxExclusive)
        {
            return false;
        }

        if (condition.MaxInclusive is { } maxInclusive && ratio > maxInclusive)
        {
            return false;
        }

        return true;
    }
}
