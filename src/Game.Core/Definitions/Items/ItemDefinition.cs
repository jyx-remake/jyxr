using System.Text.Json.Serialization;
using Game.Core.Model;

namespace Game.Core.Definitions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "category")]
[JsonDerivedType(typeof(NormalItemDefinition), "normal")]
[JsonDerivedType(typeof(EquipmentDefinition), "equipment")]
public abstract record ItemDefinition
{
    public required string Id { get; init; }
    public required string Name  { get; init; }
    public ItemType Type  { get; init; }
    public int Level { get; init; }
    public int Price { get; init; }
    public int Cooldown { get; init; }
    public bool CanDrop { get; init; }
    public string Description { get; init; } = "";
    public string Picture { get; init; } = "";
    public IReadOnlyList<ItemRequirementDefinition> Requirements { get; init; } = [];
    public IReadOnlyList<ItemUseEffectDefinition> UseEffects { get; init; } = [];
}

public sealed record NormalItemDefinition : ItemDefinition;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StatItemRequirementDefinition), "stat")]
[JsonDerivedType(typeof(TalentItemRequirementDefinition), "talent")]
public abstract record ItemRequirementDefinition;

public sealed record StatItemRequirementDefinition(
    StatType StatId,
    int Value) : ItemRequirementDefinition;

public sealed record TalentItemRequirementDefinition(
    string TalentId) : ItemRequirementDefinition;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AddBuffItemUseEffectDefinition), "add_buff")]
[JsonDerivedType(typeof(AddRageItemUseEffectDefinition), "add_rage")]
[JsonDerivedType(typeof(DetoxifyItemUseEffectDefinition), "detoxify")]
[JsonDerivedType(typeof(AddMaxHpItemUseEffectDefinition), "add_maxhp")]
[JsonDerivedType(typeof(AddMaxMpItemUseEffectDefinition), "add_maxmp")]
[JsonDerivedType(typeof(AddHpItemUseEffectDefinition), "add_hp")]
[JsonDerivedType(typeof(AddMpItemUseEffectDefinition), "add_mp")]
[JsonDerivedType(typeof(AddHpPercentItemUseEffectDefinition), "add_hp_percent")]
[JsonDerivedType(typeof(AddMpPercentItemUseEffectDefinition), "add_mp_percent")]
[JsonDerivedType(typeof(GrantExternalSkillItemUseEffectDefinition), "external_skill")]
[JsonDerivedType(typeof(GrantInternalSkillItemUseEffectDefinition), "internal_skill")]
[JsonDerivedType(typeof(GrantSpecialSkillItemUseEffectDefinition), "special_skill")]
[JsonDerivedType(typeof(GrantTalentItemUseEffectDefinition), "grant_talent")]
public abstract record ItemUseEffectDefinition;

public sealed record AddBuffItemUseEffectDefinition(
    string BuffId,
    int Level = 1,
    int Duration = 3,
    int? Property = null) : ItemUseEffectDefinition;

public sealed record AddRageItemUseEffectDefinition(
    int Value) : ItemUseEffectDefinition;

public sealed record DetoxifyItemUseEffectDefinition(
    IReadOnlyList<int>? Values = null) : ItemUseEffectDefinition;

public sealed record AddMaxHpItemUseEffectDefinition(
    int Value) : ItemUseEffectDefinition;

public sealed record AddMaxMpItemUseEffectDefinition(
    int Value) : ItemUseEffectDefinition;

public sealed record AddHpItemUseEffectDefinition(
    int Value) : ItemUseEffectDefinition;

public sealed record AddMpItemUseEffectDefinition(
    int Value) : ItemUseEffectDefinition;

public sealed record AddHpPercentItemUseEffectDefinition(
    int Value) : ItemUseEffectDefinition;

public sealed record AddMpPercentItemUseEffectDefinition(
    int Value) : ItemUseEffectDefinition;

public sealed record GrantExternalSkillItemUseEffectDefinition(
    string SkillId,
    int? Level = null) : ItemUseEffectDefinition;

public sealed record GrantInternalSkillItemUseEffectDefinition(
    string SkillId,
    int? Level = null) : ItemUseEffectDefinition;

public sealed record GrantSpecialSkillItemUseEffectDefinition(
    string SkillId) : ItemUseEffectDefinition;

public sealed record GrantTalentItemUseEffectDefinition(
    string TalentId) : ItemUseEffectDefinition;
