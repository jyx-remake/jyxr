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
    PostAdd
}

public enum HookTiming
{
    OnBattleStart,
    BeforeActionStart,
    AfterActionEnd,
    BeforeMove,
    AfterMove,
    BeforeSkillCost,
    BeforeDamageCalculation,
    BeforeSkillCast,
    AfterSkillCast,
    AfterSkillHit,
    BeforeItemUse,
    AfterItemUse,
    BeforeRest,
    AfterRest,
    BeforeBuffApplied,
    OnBuffApplied,
    OnBuffExpired,
    OnDamageTaken
}

public enum TraitId
{
    Swift,
    IgnoreZoneOfControl,
    CanUseItemOnAlly,
    IgnoreItemCooldown,
    Ghost
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

public enum BattleHookSpeechSpeaker
{
    [JsonStringEnumMemberName("hook_owner")]
    HookOwner,
    [JsonStringEnumMemberName("source")]
    Source,
    [JsonStringEnumMemberName("target")]
    Target
}

public readonly record struct ModifierValue(ModifierOp Op, double Delta)
{
    public static ModifierValue Add(double delta) => new(ModifierOp.Add, delta);

    public static ModifierValue Increase(double delta) => new(ModifierOp.Increase, delta);

    public static ModifierValue More(double delta) => new(ModifierOp.More, delta);

    public static ModifierValue PostAdd(double delta) => new(ModifierOp.PostAdd, delta);
}
