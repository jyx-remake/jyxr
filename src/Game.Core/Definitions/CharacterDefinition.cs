using System.Text.Json.Serialization;
using Game.Core.Abstractions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Core.Definitions;

[method: JsonConstructor]
public sealed record CharacterDefinition(
    string Id,
    string Name,
    IReadOnlyDictionary<StatType, int> Stats,
    IReadOnlyList<InitialExternalSkillEntryDefinition> ExternalSkills,
    IReadOnlyList<InitialInternalSkillEntryDefinition> InternalSkills,
    IReadOnlyList<string> TalentIds,
    IReadOnlyList<string> EquipmentIds,
    int Level = 1,
    string? Portrait = null,
    string? Model = null,
    CharacterGender Gender = CharacterGender.Neutral,
    string? GrowTemplate = null,
    bool ArenaEnabled = false,
    IReadOnlyList<string>? SpecialSkillIds = null)
{
    [JsonIgnore]
    public IReadOnlyList<TalentDefinition> Talents { get; private set; } = [];

    [JsonIgnore]
    public IReadOnlyList<EquipmentDefinition> Equipments { get; private set; } = [];

    [JsonIgnore]
    public IReadOnlyList<SpecialSkillDefinition> SpecialSkills { get; private set; } = [];

    [JsonIgnore]
    private bool IsResolved { get; set; }

    public void Resolve(IContentRepository contentRepository)
    {
        if (IsResolved)
        {
            return;
        }

        foreach (var skill in ExternalSkills)
        {
            skill.Resolve(contentRepository);
        }

        foreach (var skill in InternalSkills)
        {
            skill.Resolve(contentRepository);
        }

        Talents = TalentIds.Select(contentRepository.GetTalent).ToList();
        Equipments = EquipmentIds.Select(contentRepository.GetEquipment).ToList();
        SpecialSkills = (SpecialSkillIds ?? []).Select(contentRepository.GetSpecialSkill).ToList();
        IsResolved = true;
    }
}
