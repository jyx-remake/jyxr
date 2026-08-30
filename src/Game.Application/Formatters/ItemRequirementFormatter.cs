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
                statRequirement.Negated
                    ? $"{FormatterTextCn.GetStatNameCn(statRequirement.StatId)} < {statRequirement.Value}"
                    : $"{FormatterTextCn.GetStatNameCn(statRequirement.StatId)} >= {statRequirement.Value}",
            LevelItemRequirementDefinition levelRequirement =>
                $"等级 >= {levelRequirement.Value}",
            TalentItemRequirementDefinition talentRequirement =>
                $"需要天赋「{FormatterTextCn.ResolveTalentName(talentRequirement.TalentId, contentRepository)}」",
            NotTalentItemRequirementDefinition notTalentRequirement =>
                $"不能拥有天赋「{FormatterTextCn.ResolveTalentName(notTalentRequirement.TalentId, contentRepository)}」",
            RoleKeyItemRequirementDefinition roleKeyRequirement =>
                $"仅限角色「{roleKeyRequirement.CharacterId}」使用",
            GenderItemRequirementDefinition genderRequirement =>
                $"性别仅限{string.Join("、", genderRequirement.Genders.Select(FormatterTextCn.GetGenderNameCn))}",
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
