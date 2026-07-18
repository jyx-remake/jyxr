using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Presentation.Items;

public sealed record ItemCategoryOption(string Key, string DisplayName, ItemType? ItemType);

public static class ItemCatalogPresentation
{
    public static IReadOnlyList<ItemCategoryOption> Categories { get; } =
    [
        new("All", "全部", null),
        new("Equipment", "装备", ItemType.Equipment),
        new("SkillBook", "秘籍", ItemType.SkillBook),
        new("SpecialSkillBook", "特技书", ItemType.SpecialSkillBook),
        new("TalentBook", "天赋书", ItemType.TalentBook),
        new("Consumable", "消耗品", ItemType.Consumable),
        new("Booster", "强化道具", ItemType.Booster),
        new("Utility", "功能道具", ItemType.Utility),
        new("QuestItem", "剧情物品", ItemType.QuestItem),
    ];

    public static IReadOnlyList<ItemTagDefinition> GetAvailableTags(
        IEnumerable<ItemDefinition> items,
        ItemType? itemType)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (itemType is null)
        {
            return [];
        }

        return items
            .Where(item => item.Type == itemType.Value)
            .SelectMany(item => item.Tags)
            .DistinctBy(tag => tag.Id, StringComparer.Ordinal)
            .OrderBy(tag => tag.Order)
            .ThenBy(tag => tag.Id, StringComparer.Ordinal)
            .ToList();
    }

    public static bool Matches(ItemDefinition item, ItemType? itemType, string? tagId)
    {
        ArgumentNullException.ThrowIfNull(item);
        return (itemType is null || item.Type == itemType.Value) &&
               (tagId is null || item.Tags.Any(tag => string.Equals(tag.Id, tagId, StringComparison.Ordinal)));
    }

    public static string FormatCategory(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var itemTypeName = GetItemTypeName(item.Type);
        if (item.Tags.Count == 0)
        {
            return itemTypeName;
        }

        var tagNames = item.Tags
            .OrderBy(tag => tag.Order)
            .ThenBy(tag => tag.Id, StringComparer.Ordinal)
            .Select(tag => tag.Name);
        return $"{itemTypeName} · {string.Join(" / ", tagNames)}";
    }

    public static string GetItemTypeName(ItemType itemType) =>
        Categories.First(category => category.ItemType == itemType).DisplayName;
}
