using System.Text.Json.Serialization;
using Game.Core.Abstractions;
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
    public required bool ConsumeOnUse { get; init; }
    public int Level { get; init; }
    public int Price { get; init; }
    public int Cooldown { get; init; }
    public bool CanDrop { get; init; }
    public string Description { get; init; } = "";
    public string Picture { get; init; } = "";
    public IReadOnlyList<string> TagIds { get; init; } = [];
    [JsonIgnore]
    public IReadOnlyList<ItemTagDefinition> Tags { get; private set; } = [];
    public IReadOnlyList<ItemRequirementDefinition> Requirements { get; init; } = [];
    public IReadOnlyList<ItemUseEffectDefinition> UseEffects { get; init; } = [];
    /// <summary>
    /// Legacy <c>box</c> override: false means never stashable (box -1),
    /// true means always stashable (box 1), null defers to <see cref="Type"/>.
    /// </summary>
    public bool? AllowChestStorage { get; init; }

    public void ResolveTags(IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);
        Tags = TagIds.Select(contentRepository.GetItemTag).ToList();
    }
}

public sealed record NormalItemDefinition : ItemDefinition;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StatItemRequirementDefinition), "stat")]
[JsonDerivedType(typeof(LevelItemRequirementDefinition), "level")]
[JsonDerivedType(typeof(TalentItemRequirementDefinition), "talent")]
[JsonDerivedType(typeof(NotTalentItemRequirementDefinition), "not_talent")]
[JsonDerivedType(typeof(RoleKeyItemRequirementDefinition), "role_key")]
[JsonDerivedType(typeof(GenderItemRequirementDefinition), "gender")]
public abstract record ItemRequirementDefinition;

public sealed record StatItemRequirementDefinition(
    StatType StatId,
    int Value,
    ItemRequirementStatSource? Source = null,
    bool Negated = false) : ItemRequirementDefinition;

public sealed record LevelItemRequirementDefinition(
    int Value) : ItemRequirementDefinition;

public sealed record TalentItemRequirementDefinition(
    string TalentId) : ItemRequirementDefinition;

public sealed record NotTalentItemRequirementDefinition(
    string TalentId) : ItemRequirementDefinition;

public sealed record RoleKeyItemRequirementDefinition(
    string CharacterId) : ItemRequirementDefinition;

public sealed record GenderItemRequirementDefinition(
    IReadOnlyList<CharacterGender> Genders) : ItemRequirementDefinition;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AddBuffItemUseEffectDefinition), "add_buff")]
[JsonDerivedType(typeof(AddRageItemUseEffectDefinition), "add_rage")]
[JsonDerivedType(typeof(DetoxifyItemUseEffectDefinition), "detoxify")]
[JsonDerivedType(typeof(AddStatsItemUseEffectDefinition), "add_stats")]
[JsonDerivedType(typeof(AddHpItemUseEffectDefinition), "add_hp")]
[JsonDerivedType(typeof(AddMpItemUseEffectDefinition), "add_mp")]
[JsonDerivedType(typeof(AddHpPercentItemUseEffectDefinition), "add_hp_percent")]
[JsonDerivedType(typeof(AddMpPercentItemUseEffectDefinition), "add_mp_percent")]
[JsonDerivedType(typeof(GrantExternalSkillItemUseEffectDefinition), "external_skill")]
[JsonDerivedType(typeof(GrantInternalSkillItemUseEffectDefinition), "internal_skill")]
[JsonDerivedType(typeof(GrantSpecialSkillItemUseEffectDefinition), "special_skill")]
[JsonDerivedType(typeof(GrantTalentItemUseEffectDefinition), "grant_talent")]
[JsonDerivedType(typeof(GrantTitleItemUseEffectDefinition), "grant_title")]
[JsonDerivedType(typeof(SetGenderItemUseEffectDefinition), "set_gender")]
[JsonDerivedType(typeof(SetPortraitItemUseEffectDefinition), "set_portrait")]
[JsonDerivedType(typeof(ClearBuffsItemUseEffectDefinition), "clear_buffs")]
[JsonDerivedType(typeof(RandomItemItemUseEffectDefinition), "random_item")]
[JsonDerivedType(typeof(ReduceMaxResourceRatioItemUseEffectDefinition), "reduce_max_resource_ratio")]
[JsonDerivedType(typeof(RunStoryItemUseEffectDefinition), "run_story")]
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

public sealed record AddStatsItemUseEffectDefinition(
    IReadOnlyDictionary<StatType, int> Values) : ItemUseEffectDefinition;

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

public sealed record GrantTitleItemUseEffectDefinition(
    string TitleId) : ItemUseEffectDefinition;

public sealed record SetGenderItemUseEffectDefinition(
    CharacterGender Gender) : ItemUseEffectDefinition;

public sealed record SetPortraitItemUseEffectDefinition(
    string PictureId) : ItemUseEffectDefinition;

public sealed record ClearBuffsItemUseEffectDefinition : ItemUseEffectDefinition;

public sealed record RandomItemEntry(
    string ItemId,
    int Quantity);

public sealed record RandomItemItemUseEffectDefinition(
    IReadOnlyList<RandomItemEntry> Items) : ItemUseEffectDefinition;

public sealed record ReduceMaxResourceRatioItemUseEffectDefinition(
    StatType StatId,
    double Ratio) : ItemUseEffectDefinition;

public sealed record RunStoryItemUseEffectDefinition(
    string StoryId,
    bool KeepItem = false) : ItemUseEffectDefinition;
