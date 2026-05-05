using System.Text.Json.Serialization;
using Game.Core.Abstractions;
using Game.Core.Affix;

namespace Game.Core.Definitions.Skills;

public sealed record InternalSkillDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = "";

    public string Icon { get; init; } = "";

    public int Yin { get; init; }

    public int Yang { get; init; }

    public double AttackScale { get; init; }

    public double CriticalScale { get; init; }

    public double DefenceScale { get; init; }

    public double Hard { get; init; } = 1d;

    public IReadOnlyList<FormSkillDefinition> FormSkills { get; init; } = [];

    public IReadOnlyList<SkillAffixDefinition> Affixes { get; init; } = [];

    public void Resolve(IContentRepository contentRepository)
    {
        foreach (var formSkill in FormSkills)
        {
            formSkill.Resolve(contentRepository);
        }
    }
}
