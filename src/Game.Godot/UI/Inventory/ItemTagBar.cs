using Game.Core.Definitions;
using Game.Core.Model;
using Game.Presentation.Items;
using Godot;

namespace Game.Godot.UI;

public partial class ItemTagBar : HFlowContainer
{
    [Export]
    public PackedScene TagButtonScene { get; set; } = null!;

    private readonly ItemCatalogFilter _filter = new();

    public event Action? SelectionChanged;
    public string? SelectedTagId => _filter.SelectedTagId;

    public void SetItems(IEnumerable<ItemDefinition> items, ItemType? itemType)
    {
        var previousType = _filter.ItemType;
        var previousTag = _filter.SelectedTagId;
        var previousTags = _filter.AvailableTags;
        _filter.UpdateItems(items, itemType);
        if (previousType != itemType || previousTag != SelectedTagId
            || !previousTags.SequenceEqual(_filter.AvailableTags) || GetChildCount() == 0)
        {
            RefreshButtons();
        }
    }

    public void ResetSelection()
    {
        _filter.SelectTag(null);
        RefreshButtons();
    }

    public bool Matches(ItemDefinition item) => _filter.Matches(item);

    private void RefreshButtons()
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        Visible = _filter.ItemType is not null;
        if (!Visible)
        {
            return;
        }

        AddButton(null, "全部");
        foreach (var tag in _filter.AvailableTags)
        {
            AddButton(tag.Id, tag.Name);
        }
    }

    private void AddButton(string? tagId, string text)
    {
        if (TagButtonScene is null)
        {
            throw new InvalidOperationException("TagButtonScene is not assigned.");
        }
        var instance = TagButtonScene.Instantiate();
        if (instance is not InventoryTagButton button)
        {
            instance.QueueFree();
            throw new InvalidOperationException("Tag button scene root must be InventoryTagButton.");
        }
        button.Configure(text, SelectedTagId == tagId, () =>
        {
            _filter.SelectTag(tagId);
            RefreshButtons();
            SelectionChanged?.Invoke();
        });
        AddChild(button);
    }
}
