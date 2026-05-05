using System.Text.Json.Serialization;
using Game.Core.Abstractions;

namespace Game.Core.Definitions.Skills;

[method: JsonConstructor]
public sealed record InitialExternalSkillEntryDefinition(
    string Id,
    int Level = 1,
    int? MaxLevel = null)
{
    [JsonIgnore]
    public ExternalSkillDefinition Skill { get; private set; } = null!;

    public InitialExternalSkillEntryDefinition(
        ExternalSkillDefinition skill,
        int Level = 1,
        int? MaxLevel = null)
        : this(skill.Id, Level, MaxLevel)
    {
        Skill = skill;
    }

    public void Resolve(IContentRepository contentRepository)
    {
        Skill = contentRepository.GetExternalSkill(Id);
    }
}

[method: JsonConstructor]
public sealed record InitialInternalSkillEntryDefinition(
    string Id,
    int Level = 1,
    int? MaxLevel = null,
    bool Equipped = false)
{
    [JsonIgnore]
    public InternalSkillDefinition Skill { get; private set; } = null!;

    public InitialInternalSkillEntryDefinition(
        InternalSkillDefinition skill,
        int Level = 1,
        int? MaxLevel = null,
        bool Equipped = false)
        : this(skill.Id, Level, MaxLevel, Equipped)
    {
        Skill = skill;
    }

    public void Resolve(IContentRepository contentRepository)
    {
        Skill = contentRepository.GetInternalSkill(Id);
    }
}
