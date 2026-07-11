using Game.Core.Affix;
using Game.Core.Abstractions;
using Game.Core;

namespace Game.Core.Battle;

public sealed class BattleHookExecutor
{
    internal BattleEffectExecutor? EffectExecutor { get; set; }

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
            using var effectScope = context.State.EnterEffect();
            (EffectExecutor ?? throw new InvalidOperationException("Battle hook executor is not attached to a battle engine."))
                .ExecuteHook(context, effect);
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
            ContextRecoveryKindBattleHookConditionDefinition recoveryKind =>
                context.RecoveryKind == recoveryKind.Kind,
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
                case ModifyRecoveryBattleHookEffectDefinition:
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
                case ExtraStrikeBattleHookEffectDefinition:
                    throw new InvalidOperationException(
                        $"Preview battle hook execution does not support side-effect effect '{effect.GetType().Name}' on timing '{hook.Timing}'.");
                case CustomBattleEffectDefinition customEffect when customEffect.SupportsPreview:
                    continue;
                case CustomBattleEffectDefinition:
                    throw new InvalidOperationException(
                        $"Preview battle hook execution does not support side-effect effect '{effect.GetType().Name}' on timing '{hook.Timing}'.");
                default:
                    throw new NotSupportedException(
                        $"Unsupported preview battle hook effect '{effect.GetType().Name}'.");
            }
        }
    }

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
