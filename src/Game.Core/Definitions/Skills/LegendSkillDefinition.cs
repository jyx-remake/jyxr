using Game.Core.Abstractions;

namespace Game.Core.Definitions.Skills;

public sealed record LegendSkillDefinition(
    string Id,
    string Name,
    string StartSkill,
    double Probability,
    IReadOnlyList<LegendSkillConditionDefinition> Conditions,
    IReadOnlyList<SkillBuffDefinition> Buffs,
    double PowerExtra = 0d,
    int RequiredLevel = 1,
    string? Animation = null)
{
    public void Resolve(IContentRepository contentRepository)
    {
        foreach (var buff in Buffs)
        {
            buff.Resolve(contentRepository);
        }
    }
}
