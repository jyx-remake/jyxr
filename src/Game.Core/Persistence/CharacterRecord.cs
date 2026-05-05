using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Persistence;

public sealed record ExternalSkillRecord(
    string ExternalSkillDefinitionId,
    int Level,
    int Exp,
    bool IsActive);

public sealed record InternalSkillRecord(
    string InternalSkillDefinitionId,
    int Level,
    int Exp,
    bool Equipped);

public sealed record SpecialSkillRecord(
    string SpecialSkillDefinitionId,
    bool IsActive);

public sealed record EquipmentRecord(
    string Id,
    string EquipmentDefinitionId,
    IReadOnlyList<AffixDefinition> ExtraAffixes);

public sealed record CharacterRecord(
    string Id,
    string DefinitionId,
    string Name,
    string? Portrait,
    string? Model,
    string? GrowTemplateId,
    int Level,
    int Experience,
    int UnspentStatPoints,
    IReadOnlyDictionary<StatType, int> BaseStats,
    IReadOnlyList<string> UnlockedTalentIds,
    IReadOnlyList<SpecialSkillRecord> SpecialSkills,
    IReadOnlyList<ExternalSkillRecord> ExternalSkills,
    IReadOnlyList<InternalSkillRecord> InternalSkills,
    IReadOnlyDictionary<EquipmentSlotType, EquipmentRecord> EquippedItems);
