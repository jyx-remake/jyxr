using System.Text.Json.Serialization;

namespace Game.Core.Affix;

public enum ProviderKind
{
    Equipment,
    ExternalSkill,
    InternalSkill,
    Talent,
    Buff,
    Other
}

public enum ModifierOp
{
    [JsonStringEnumMemberName("add")]
    Add,
    [JsonStringEnumMemberName("increase")]
    Increase,
    [JsonStringEnumMemberName("more")]
    More,
    [JsonStringEnumMemberName("post_add")]
    PostAdd,
    [JsonStringEnumMemberName("override")]
    Override
}

public enum HookTiming
{
    OnBattleStart,
    BeforeActionReadiness,
    BeforeActionStart,
    AfterActionEnd,
    AfterBuffRound,
    BeforeMove,
    AfterMove,
    BeforeSkillCost,
    BeforeHitResolved,
    BeforeDamageCalculation,
    BeforeDamageApplied,
    BeforeDefeated,
    BeforeSkillCast,
    AfterSkillCast,
    OnHitConfirmed,
    BeforeItemUse,
    AfterItemUse,
    BeforeRest,
    AfterRest,
    BeforeBuffApplied,
    OnBuffApplied,
    OnBuffRemoved,
    OnDamageTaken,
    OnDamageDealt,
    BeforeRecoveryResolved
}

public enum SkillTargetingField
{
    [JsonStringEnumMemberName("cast_size")]
    CastSize,
    [JsonStringEnumMemberName("impact_size")]
    ImpactSize
}

public readonly record struct SkillTargetingModifierKey(string? SourceSkillId, SkillTargetingField Field);

public enum BattleRecoveryKind
{
    [JsonStringEnumMemberName("hp")]
    Hp,
    [JsonStringEnumMemberName("mp")]
    Mp
}

public enum TraitId
{
    Swift,
    IgnoreZoneOfControl,
    CanUseItemOnAlly,
    IgnoreItemCooldown,
    Ghost,
    BroadLearning,
    DoubleExperienceGain,
    DoubleSkillEquipmentTenDimensionAffixes,
    IncreaseInternalSkillYangAffinity,
    MindEye,
    PoisonResistance,
    AvoidFriendlyFire,
    Irascible,
    DoubleCombatRageGain,
    CannotMove,
}

public enum BattleDamageContextField
{
    [JsonStringEnumMemberName("source_attack")]
    SourceAttack,
    [JsonStringEnumMemberName("source_attack_low")]
    SourceAttackLow,
    [JsonStringEnumMemberName("source_attack_high")]
    SourceAttackHigh,
    [JsonStringEnumMemberName("target_defence")]
    TargetDefence,
    [JsonStringEnumMemberName("critical_chance")]
    CriticalChance,
    [JsonStringEnumMemberName("critical_multiplier")]
    CriticalMultiplier,
    [JsonStringEnumMemberName("final_damage")]
    FinalDamage
}

public enum BattleHookContextUnitRole
{
    [JsonStringEnumMemberName("source")]
    Source,
    [JsonStringEnumMemberName("target")]
    Target
}

public enum BattleHitState
{
    [JsonStringEnumMemberName("hit")]
    Hit,
    [JsonStringEnumMemberName("miss")]
    Miss
}

public enum BattleHookRelation
{
    [JsonStringEnumMemberName("ally")]
    Ally,
    [JsonStringEnumMemberName("enemy")]
    Enemy
}

public readonly record struct ModifierValue(ModifierOp Op, double Delta)
{
    public static ModifierValue Add(double delta) => new(ModifierOp.Add, delta);

    public static ModifierValue Increase(double delta) => new(ModifierOp.Increase, delta);

    public static ModifierValue More(double delta) => new(ModifierOp.More, delta);

    public static ModifierValue PostAdd(double delta) => new(ModifierOp.PostAdd, delta);

    public static ModifierValue Override(double value) => new(ModifierOp.Override, value);
}
