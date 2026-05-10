using System.Text.Json.Serialization;
using Game.Core.Model;

namespace Game.Core.Definitions;

public sealed record EquipmentRandomAffixTableDefinition
{
    public int MinItemLevel { get; init; }

    public int MaxItemLevel { get; init; }

    public IReadOnlyList<EquipmentRandomAffixOptionDefinition> Options { get; init; } = [];
}

public sealed record EquipmentRandomAffixOptionDefinition
{
    public EquipmentRandomAffixKind Kind { get; init; }

    public int Weight { get; init; }

    public WeaponType? WeaponType { get; init; }

    public IReadOnlyList<EquipmentRandomAffixRangeDefinition> Ranges { get; init; } = [];

    public IReadOnlyList<string> Pool { get; init; } = [];
}

public sealed record EquipmentRandomAffixRangeDefinition(
    int Min,
    int Max);

public enum EquipmentRandomAffixKind
{
    [JsonStringEnumMemberName("attack_combo")]
    AttackCombo,
    [JsonStringEnumMemberName("defence_combo")]
    DefenceCombo,
    [JsonStringEnumMemberName("random_attribute")]
    RandomAttribute,
    [JsonStringEnumMemberName("talent")]
    Talent,
    [JsonStringEnumMemberName("accuracy")]
    Accuracy,
    [JsonStringEnumMemberName("external_skill_bonus")]
    ExternalSkillBonus,
    [JsonStringEnumMemberName("internal_skill_bonus")]
    InternalSkillBonus,
    [JsonStringEnumMemberName("form_skill_bonus")]
    FormSkillBonus,
    [JsonStringEnumMemberName("legend_skill_bonus")]
    LegendSkillBonus,
    [JsonStringEnumMemberName("crit_chance")]
    CritChance,
    [JsonStringEnumMemberName("crit_mult")]
    CritMult,
    [JsonStringEnumMemberName("lifesteal")]
    Lifesteal,
    [JsonStringEnumMemberName("speed")]
    Speed,
    [JsonStringEnumMemberName("anti_debuff")]
    AntiDebuff,
    [JsonStringEnumMemberName("weapon_bonus")]
    WeaponBonus,
}
