using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Presentation.Items;

public sealed class ItemCatalogFilter
{
    public ItemType? ItemType { get; private set; }
    public string? SelectedTagId { get; private set; }
    public IReadOnlyList<ItemTagDefinition> AvailableTags { get; private set; } = [];

    public void UpdateItems(IEnumerable<ItemDefinition> items, ItemType? itemType)
    {
        var tags = ItemCatalogPresentation.GetAvailableTags(items, itemType);
        if (ItemType != itemType || !tags.Any(tag => tag.Id == SelectedTagId))
        {
            SelectedTagId = null;
        }
        ItemType = itemType;
        AvailableTags = tags;
    }

    public void SelectTag(string? tagId)
    {
        if (tagId is not null && !AvailableTags.Any(tag => tag.Id == tagId))
        {
            throw new ArgumentException("Tag is outside the current catalog.", nameof(tagId));
        }
        SelectedTagId = tagId;
    }

    public bool Matches(ItemDefinition item) =>
        ItemCatalogPresentation.Matches(item, ItemType, SelectedTagId);
}
