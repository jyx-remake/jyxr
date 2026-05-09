using Game.Core.Abstractions;

namespace Game.Core.Definitions.Skills;

// TODO: Add an explicit priority field when multiple legend skills can match the same start skill.
// Current design intentionally does not implement this yet.
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
