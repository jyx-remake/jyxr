using Game.Core.Affix;

namespace Game.Core.Battle;

internal static class BattleHookEvaluator
{
    public static bool Evaluate(BattleHookContext context, BattleHookConditionDefinition condition) =>
        condition switch
        {
            ChanceBattleHookConditionDefinition chance => Probability.RollChance(context.Random, chance.Value),
            UnitLevelChanceBattleHookConditionDefinition chance => Probability.RollChance(
                context.Random,
                Math.Clamp(
                    chance.BaseValue + chance.ValuePerLevel * context.Unit.Character.Level,
                    0d,
                    chance.MaxValue)),
            DamagePositiveBattleHookConditionDefinition => context.DamageAmount is > 0,
            ContextBuffIdBattleHookConditionDefinition buffId => context.Buff is not null &&
                string.Equals(context.Buff.Definition.Id, buffId.BuffId, StringComparison.Ordinal),
            ContextBuffNegativeBattleHookConditionDefinition => context.Buff?.Definition.IsDebuff == true,
            ContextUnitHpRatioBattleHookConditionDefinition hpRatio => IsContextUnitHpRatio(context, hpRatio),
            ContextUnitEffectiveTalentBattleHookConditionDefinition talent =>
                talent.TalentIds.Any(context.Unit.Character.HasEffectiveTalent),
            ContextUnitEquippedInternalSkillBattleHookConditionDefinition internalSkill =>
                internalSkill.InternalSkillIds.Any(id => string.Equals(
                    id,
                    GetEquippedInternalSkillId(context.Unit),
                    StringComparison.Ordinal)),
            ContextUnitRelationBattleHookConditionDefinition relation => IsContextUnitRelation(context, relation),
            ContextUnitRoleBattleHookConditionDefinition unitRole => IsContextUnitRole(context, unitRole.Role),
            ContextUnitGenderBattleHookConditionDefinition gender => IsContextUnitGender(context, gender),
            ContextHitStateBattleHookConditionDefinition hitState => context.HitState == hitState.State,
            ContextSkillNameEqualsBattleHookConditionDefinition skillName => context.Skill is not null &&
                skillName.Values.Any(value => string.Equals(context.Skill.Name, value, StringComparison.Ordinal)),
            ContextSkillNameContainsBattleHookConditionDefinition skillName => context.Skill is not null &&
                skillName.Values.Any(value => context.Skill.Name.Contains(value, StringComparison.Ordinal)),
            ContextSkillKindBattleHookConditionDefinition skillKind => context.Skill is not null &&
                skillKind.Kinds.Contains(context.Skill.SkillKind),
            ContextSkillWeaponTypeBattleHookConditionDefinition skillWeaponType => context.Skill is not null &&
                skillWeaponType.WeaponTypes.Contains(context.Skill.WeaponType),
            ContextRecoveryKindBattleHookConditionDefinition recoveryKind => context.RecoveryKind == recoveryKind.Kind,
            _ => throw new NotSupportedException($"Unsupported battle hook condition '{condition.GetType().Name}'."),
        };

    private static string? GetEquippedInternalSkillId(BattleUnit unit) =>
        unit.Character.GetInternalSkills()
            .FirstOrDefault(static skill => skill.IsEquipped)
            ?.Definition.Id;

    private static bool IsContextUnitRole(BattleHookContext context, BattleHookContextUnitRole role) =>
        role switch
        {
            BattleHookContextUnitRole.Source => context.Source is not null && context.Unit.Id == context.Source.Id,
            BattleHookContextUnitRole.Target => context.Target is not null && context.Unit.Id == context.Target.Id,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };

    private static bool IsContextUnitRelation(
        BattleHookContext context,
        ContextUnitRelationBattleHookConditionDefinition condition)
    {
        var otherUnit = condition.Role switch
        {
            BattleHookContextUnitRole.Source => context.Source,
            BattleHookContextUnitRole.Target => context.Target,
            _ => throw new ArgumentOutOfRangeException(nameof(condition.Role), condition.Role, null),
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
            _ => throw new ArgumentOutOfRangeException(nameof(condition.Relation), condition.Relation, null),
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
            _ => throw new ArgumentOutOfRangeException(nameof(condition.Role), condition.Role, null),
        };
        return unit is not null && condition.Genders.Contains(unit.Character.Definition.Gender);
    }

    private static bool IsContextUnitHpRatio(
        BattleHookContext context,
        ContextUnitHpRatioBattleHookConditionDefinition condition)
    {
        var ratio = context.Unit.MaxHp <= 0 ? 0d : (double)context.Unit.Hp / context.Unit.MaxHp;
        return (condition.MinInclusive is null || ratio >= condition.MinInclusive) &&
            (condition.MinExclusive is null || ratio > condition.MinExclusive) &&
            (condition.MaxExclusive is null || ratio < condition.MaxExclusive) &&
            (condition.MaxInclusive is null || ratio <= condition.MaxInclusive);
    }
}
