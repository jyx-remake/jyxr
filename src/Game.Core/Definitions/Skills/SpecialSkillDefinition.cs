using Game.Core.Abstractions;

namespace Game.Core.Definitions.Skills;

public sealed record SpecialSkillDefinition(
    string Id,
    string Name,
    string Description,
    string Icon,
    int Cooldown,
    SkillCostDefinition Cost,
    SkillTargetingDefinition? Targeting,
    string Animation,
    string Audio,
    IReadOnlyList<SkillBuffDefinition> Buffs)
{
    public void Resolve(IContentRepository contentRepository)
    {
        foreach (var buff in Buffs)
        {
            buff.Resolve(contentRepository);
        }
    }
}
