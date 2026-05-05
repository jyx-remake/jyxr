using Game.Core.Abstractions;
using Game.Core.Definitions;

namespace Game.Application.Formatters;

public static class ItemRequirementFormatter
{
    public static string FormatCn(ItemRequirementDefinition requirement, IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(contentRepository);

        return requirement switch
        {
            StatItemRequirementDefinition statRequirement =>
                $"{FormatterTextCn.GetStatNameCn(statRequirement.StatId)} >= {statRequirement.Value}",
            TalentItemRequirementDefinition talentRequirement =>
                $"需要天赋「{FormatterTextCn.ResolveTalentName(talentRequirement.TalentId, contentRepository)}」",
            _ => throw new NotSupportedException($"Unsupported item requirement type '{requirement.GetType().Name}'.")
        };
    }

    public static IReadOnlyList<string> FormatLinesCn(
        IEnumerable<ItemRequirementDefinition> requirements,
        IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(contentRepository);

        return requirements.Select(requirement => FormatCn(requirement, contentRepository)).ToList();
    }
}
