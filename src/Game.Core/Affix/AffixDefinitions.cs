using System.Text.Json.Serialization;
using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Core.Affix;

public interface IAffixProvider
{
    ProviderKind ProviderKind { get; }

    IReadOnlyList<AffixDefinition> Affixes { get; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StatModifierAffix), "stat_modifier")]
[JsonDerivedType(typeof(GrantTalentAffix), "grant_talent")]
[JsonDerivedType(typeof(GrantModelAffix), "grant_model")]
[JsonDerivedType(typeof(SkillBonusModifierAffix), "skill_bonus_modifier")]
[JsonDerivedType(typeof(WeaponBonusModifierAffix), "weapon_bonus_modifier")]
[JsonDerivedType(typeof(LegendSkillChanceModifierAffix), "legend_skill_chance_modifier")]
[JsonDerivedType(typeof(BuffLevelStatModifierAffix), "buff_level_stat_modifier")]
[JsonDerivedType(typeof(HookAffix), "hook")]
[JsonDerivedType(typeof(TraitAffix), "trait")]
public abstract record AffixDefinition
{
    [JsonIgnore]
    public ProviderKind SourceKind { get; internal init; } = ProviderKind.Other;

    public virtual void Resolve(IContentRepository contentRepository)
    {
    }
}

public sealed record SkillAffixDefinition(
    AffixDefinition Effect,
    int MinimumLevel = 1,
    bool RequiresEquippedInternalSkill = false);

public sealed record StatModifierAffix(StatType Stat, ModifierValue Value) : AffixDefinition;

public sealed record GrantTalentAffix(string TalentId) : AffixDefinition
{
    [JsonIgnore]
    public TalentDefinition Talent { get; private set; } = null!;

    public override void Resolve(IContentRepository contentRepository)
    {
        Talent = contentRepository.GetTalent(TalentId);
    }
}

public sealed record GrantModelAffix(string ModelId, int Priority, string Description = "") : AffixDefinition;

public sealed record SkillBonusModifierAffix(string SkillId, ModifierValue Value) : AffixDefinition;

public sealed record WeaponBonusModifierAffix(WeaponType WeaponType, ModifierValue Value) : AffixDefinition;

public sealed record LegendSkillChanceModifierAffix(string SkillId, ModifierValue Value) : AffixDefinition;

public sealed record BuffLevelStatModifierAffix(
    StatType Stat,
    double AddBase = 0d,
    double AddPerLevel = 0d,
    double MulPerLevel = 0d) : AffixDefinition;

public sealed record HookAffix : AffixDefinition
{
    public required HookTiming Timing { get; init; }

    public IReadOnlyList<BattleHookConditionDefinition> Conditions { get; init; } = [];

    public IReadOnlyList<BattleHookEffectDefinition> Effects { get; init; } = [];

    public BattleHookSpeechDefinition? Speech { get; init; }
}

public sealed record TraitAffix(TraitId TraitId) : AffixDefinition;

public sealed record BattleHookSpeechDefinition
{
    public BattleHookSpeechSpeaker Speaker { get; init; } = BattleHookSpeechSpeaker.HookOwner;
    public required IReadOnlyList<string> Lines { get; init; }
    public double Chance { get; init; } = 1d;
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ChanceBattleHookConditionDefinition), "chance")]
[JsonDerivedType(typeof(DamagePositiveBattleHookConditionDefinition), "damage_positive")]
[JsonDerivedType(typeof(ContextBuffIdBattleHookConditionDefinition), "context_buff_id")]
[JsonDerivedType(typeof(ContextUnitHpRatioBattleHookConditionDefinition), "context_unit_hp_ratio")]
[JsonDerivedType(typeof(ContextUnitEffectiveTalentBattleHookConditionDefinition), "context_unit_effective_talent")]
[JsonDerivedType(typeof(ContextUnitEquippedInternalSkillBattleHookConditionDefinition), "context_unit_equipped_internal_skill")]
[JsonDerivedType(typeof(ContextUnitRoleBattleHookConditionDefinition), "context_unit_role")]
[JsonDerivedType(typeof(ContextSkillNameContainsBattleHookConditionDefinition), "context_skill_name_contains")]
[JsonDerivedType(typeof(ContextSkillWeaponTypeBattleHookConditionDefinition), "context_skill_weapon_type")]
public abstract record BattleHookConditionDefinition;

public sealed record ChanceBattleHookConditionDefinition(double Value) : BattleHookConditionDefinition;

public sealed record DamagePositiveBattleHookConditionDefinition : BattleHookConditionDefinition;

public sealed record ContextBuffIdBattleHookConditionDefinition(string BuffId) : BattleHookConditionDefinition;

public sealed record ContextUnitHpRatioBattleHookConditionDefinition(
    double? MinExclusive = null,
    double? MaxInclusive = null) : BattleHookConditionDefinition;

public sealed record ContextUnitEffectiveTalentBattleHookConditionDefinition(
    IReadOnlyList<string> TalentIds) : BattleHookConditionDefinition;

public sealed record ContextUnitEquippedInternalSkillBattleHookConditionDefinition(
    IReadOnlyList<string> InternalSkillIds) : BattleHookConditionDefinition;

public sealed record ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole Role) : BattleHookConditionDefinition;

public sealed record ContextSkillNameContainsBattleHookConditionDefinition(
    IReadOnlyList<string> Values) : BattleHookConditionDefinition;

public sealed record ContextSkillWeaponTypeBattleHookConditionDefinition(
    IReadOnlyList<WeaponType> WeaponTypes) : BattleHookConditionDefinition;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ModifyDamageBattleHookEffectDefinition), "modify_damage")]
[JsonDerivedType(typeof(ModifyDamageContextBattleHookEffectDefinition), "modify_damage_context")]
[JsonDerivedType(typeof(ModifyMpCostBattleHookEffectDefinition), "modify_mp_cost")]
[JsonDerivedType(typeof(StrengthenContextBuffBattleHookEffectDefinition), "strengthen_context_buff")]
[JsonDerivedType(typeof(ApplyBuffBattleHookEffectDefinition), "apply_buff")]
public abstract record BattleHookEffectDefinition;

public enum BattleHookRounding
{
    Truncate,
    Floor,
    Ceiling,
    Round
}

public sealed record ModifyDamageBattleHookEffectDefinition(
    ModifierOp Op,
    double Delta = 0d,
    double DeltaPerBuffLevel = 0d,
    BattleHookRounding Rounding = BattleHookRounding.Truncate) : BattleHookEffectDefinition;

public sealed record ModifyDamageContextBattleHookEffectDefinition(
    BattleDamageContextField Field,
    ModifierOp Op,
    double Delta,
    double DeltaPerUnitLevel = 0d,
    double DeltaPerBuffLevel = 0d) : BattleHookEffectDefinition;

public sealed record ModifyMpCostBattleHookEffectDefinition(
    ModifierOp Op,
    double Delta = 0d,
    double DeltaPerBuffLevel = 0d,
    BattleHookRounding Rounding = BattleHookRounding.Ceiling) : BattleHookEffectDefinition;

public sealed record StrengthenContextBuffBattleHookEffectDefinition(
    int LevelDelta = 0,
    int TurnDelta = 0) : BattleHookEffectDefinition;

public sealed record ApplyBuffBattleHookEffectDefinition(
    BattleTargetSelectorDefinition Target,
    string BuffId,
    int Level,
    int Duration) : BattleHookEffectDefinition;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SelfBattleTargetSelectorDefinition), "self")]
[JsonDerivedType(typeof(SourceBattleTargetSelectorDefinition), "source")]
[JsonDerivedType(typeof(TargetBattleTargetSelectorDefinition), "target")]
[JsonDerivedType(typeof(NearbyAlliesBattleTargetSelectorDefinition), "nearby_allies")]
public abstract record BattleTargetSelectorDefinition;

public sealed record SelfBattleTargetSelectorDefinition : BattleTargetSelectorDefinition;

public sealed record SourceBattleTargetSelectorDefinition : BattleTargetSelectorDefinition;

public sealed record TargetBattleTargetSelectorDefinition : BattleTargetSelectorDefinition;

public sealed record NearbyAlliesBattleTargetSelectorDefinition(
    int Radius,
    bool IncludeSelf = true) : BattleTargetSelectorDefinition;
