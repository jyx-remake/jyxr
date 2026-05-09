using Game.Core.Affix;

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
            ChanceBattleHookConditionDefinition chance => context.Random.NextDouble() < chance.Value,
            DamagePositiveBattleHookConditionDefinition => context.DamageAmount is > 0,
            ContextBuffIdBattleHookConditionDefinition buffId => context.Buff is not null &&
                string.Equals(context.Buff.Definition.Id, buffId.BuffId, StringComparison.Ordinal),
            ContextUnitHpRatioBattleHookConditionDefinition hpRatio => IsContextUnitHpRatio(context, hpRatio),
            ContextUnitEffectiveTalentBattleHookConditionDefinition talent =>
                talent.TalentIds.Any(context.Unit.Character.HasEffectiveTalent),
            ContextUnitEquippedInternalSkillBattleHookConditionDefinition internalSkill =>
                internalSkill.InternalSkillIds.Any(id => string.Equals(id, GetEquippedInternalSkillId(context.Unit), StringComparison.Ordinal)),
            ContextUnitRoleBattleHookConditionDefinition unitRole => IsContextUnitRole(context, unitRole.Role),
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
                case ApplyBuffBattleHookEffectDefinition:
                    throw new InvalidOperationException(
                        $"Preview battle hook execution does not support side-effect effect '{effect.GetType().Name}' on timing '{hook.Timing}'.");
                default:
                    throw new NotSupportedException(
                        $"Unsupported preview battle hook effect '{effect.GetType().Name}'.");
            }
        }
    }

    private static void ApplyEffect(BattleHookContext context, BattleHookEffectDefinition effect)
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

            case ApplyBuffBattleHookEffectDefinition applyBuff:
                ApplyBuff(context, applyBuff);
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
        var delta = effect.Delta
            + effect.DeltaPerUnitLevel * context.Unit.Character.Level
            + effect.DeltaPerBuffLevel * (context.Buff?.Level ?? 0);
        damageCalculation.AddModifier(effect.Field, effect.Op, delta);
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

    private static void ApplyBuff(BattleHookContext context, ApplyBuffBattleHookEffectDefinition effect)
    {
        var definition = context.BuffResolver(effect.BuffId);
        var sourceUnitId = context.Source?.Id ?? context.Unit.Id;
        foreach (var target in SelectTargets(context, effect.Target))
        {
            target.ApplyBuff(new BattleBuffInstance(definition, effect.Level, effect.Duration, sourceUnitId, context.State.ActionSerial));
            context.State.AddEvent(new BattleEvent(BattleEventKind.BuffApplied, target.Id, Detail: $"{context.Timing}:{effect.BuffId}"));
        }
    }

    private static IReadOnlyList<BattleUnit> SelectTargets(BattleHookContext context, BattleTargetSelectorDefinition selector) =>
        selector switch
        {
            SelfBattleTargetSelectorDefinition => [context.Unit],
            SourceBattleTargetSelectorDefinition => context.Source is null ? [] : [context.Source],
            TargetBattleTargetSelectorDefinition => context.Target is null ? [] : [context.Target],
            NearbyAlliesBattleTargetSelectorDefinition nearbyAllies => context.State.GetLivingUnits()
                .Where(unit => unit.Team == context.Unit.Team)
                .Where(unit => nearbyAllies.IncludeSelf || !string.Equals(unit.Id, context.Unit.Id, StringComparison.Ordinal))
                .Where(unit => unit.Position.ManhattanDistanceTo(context.Unit.Position) <= nearbyAllies.Radius)
                .ToList(),
            _ => throw new NotSupportedException($"Unsupported battle target selector '{selector.GetType().Name}'.")
        };

    private static void TryRequestSpeech(
        BattleHookContext context,
        BattleHookSpeechDefinition? speech)
    {
        if (speech is null)
        {
            return;
        }

        if (context.Random.NextDouble() > speech.Chance)
        {
            return;
        }

        var speaker = ResolveSpeaker(context, speech.Speaker);
        if (speaker is null || !speaker.IsAlive || speech.Lines.Count == 0)
        {
            return;
        }

        var line = speech.Lines.Count == 1
            ? speech.Lines[0]
            : speech.Lines[context.Random.Next(0, speech.Lines.Count)];
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        context.RequestSpeech(speaker, line);
    }

    private static BattleUnit? ResolveSpeaker(BattleHookContext context, BattleHookSpeechSpeaker speaker) =>
        speaker switch
        {
            BattleHookSpeechSpeaker.HookOwner => context.Unit,
            BattleHookSpeechSpeaker.Source => context.Source,
            BattleHookSpeechSpeaker.Target => context.Target,
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

    private static bool IsContextUnitHpRatio(
        BattleHookContext context,
        ContextUnitHpRatioBattleHookConditionDefinition condition)
    {
        var ratio = context.Unit.MaxHp <= 0
            ? 0d
            : (double)context.Unit.Hp / context.Unit.MaxHp;
        if (condition.MinExclusive is { } minExclusive && ratio <= minExclusive)
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
