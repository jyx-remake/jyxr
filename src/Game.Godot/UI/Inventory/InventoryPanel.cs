using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Presentation.Items;
using Godot;

namespace Game.Godot.UI;

public partial class InventoryPanel : JyPanel
{
	[Export]
	public PackedScene InventoryItemBoxScene { get; set; } = null!;

	[Export]
	public PackedScene ItemTargetSelectionPanelScene { get; set; } = null!;
	[Export]
	public PackedScene TagButtonScene { get; set; } = null!;

	private static readonly IReadOnlyList<ItemCategoryOption> Categories = ItemCatalogPresentation.Categories;

	private readonly List<IDisposable> _subscriptions = [];
	private readonly Dictionary<string, Button> _buttonsByCategoryKey = [];

	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;
	private Label _countLabel = null!;
	private HFlowContainer _tagButtons = null!;
	private ItemCategoryOption _selectedCategory = Categories[0];
	private string? _selectedTagId;

	public override void _Ready()
	{
		base._Ready();
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_countLabel = GetNode<Label>("%CountLabel");
		_tagButtons = GetNode<HFlowContainer>("%TagButtons");

		InitializeCategoryButtons();

		_subscriptions.Add(Game.Session.Events.Subscribe<InventoryChangedEvent>(OnInventoryChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<SaveLoadedEvent>(OnSaveLoaded));
		Refresh();
	}

	public override void _ExitTree()
	{
		foreach (var subscription in _subscriptions)
		{
			subscription.Dispose();
		}

		_subscriptions.Clear();
	}

	private void SelectCategory(ItemCategoryOption category)
	{
		_selectedCategory = category;
		_selectedTagId = null;
		Refresh();
	}

	private void InitializeCategoryButtons()
	{
		var container = GetNode<VBoxContainer>("ContentRoot/CategoryButtons");
		var template = container.GetChildren().OfType<Button>().First();
		foreach (var child in container.GetChildren())
		{
			container.RemoveChild(child);
			child.QueueFree();
		}

		foreach (var category in Categories)
		{
			var button = (Button)template.Duplicate();
			button.Name = $"{category.Key}Button";
			button.UniqueNameInOwner = false;
			button.Text = category.DisplayName;
			button.CustomMinimumSize = new Vector2(200, category.ItemType is null ? 70 : 62);
			button.AddThemeFontSizeOverride("font_size", category.ItemType is null ? 50 : 34);
			button.Pressed += () => SelectCategory(category);
			container.AddChild(button);
			_buttonsByCategoryKey.Add(category.Key, button);
		}
	}

	private void Refresh()
	{
		ClearGrid();

		var sourceEntries = Game.State.Inventory.Entries;
		var tags = ResolveAvailableTags(sourceEntries.Select(entry => entry.Definition));
		UpdateCategoryButtons();
		UpdateTagButtons(tags);
		var entries = sourceEntries
			.Where(EntryMatchesSelectedCategory)
			.OrderBy(entry => entry.EntryNumber)
			.ToList();

		_countLabel.Text = $"{entries.Count} 项";
		_emptyLabel.Visible = entries.Count == 0;

		foreach (var entry in entries)
		{
			var itemBox = CreateItemBox(entry);
			_gridContainer.AddChild(itemBox);
		}
	}

	private bool EntryMatchesSelectedCategory(InventoryEntry entry) =>
		ItemCatalogPresentation.Matches(entry.Definition, _selectedCategory.ItemType, _selectedTagId);

	private IReadOnlyList<ItemTagDefinition> ResolveAvailableTags(IEnumerable<ItemDefinition> items)
	{
		var tags = ItemCatalogPresentation.GetAvailableTags(items, _selectedCategory.ItemType);
		if (_selectedTagId is not null && !tags.Any(tag => tag.Id == _selectedTagId))
		{
			_selectedTagId = null;
		}
		return tags;
	}

	private void UpdateTagButtons(IReadOnlyList<ItemTagDefinition> tags)
	{
		ClearChildren(_tagButtons);
		if (_selectedCategory.ItemType is null)
		{
			return;
		}

		foreach (var tag in tags)
		{
			AddTagButton(tag.Id, tag.Name);
		}
	}

	private void AddTagButton(string? tagId, string displayName)
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

		button.Configure(
			displayName,
			string.Equals(_selectedTagId, tagId, StringComparison.Ordinal),
			() =>
			{
				_selectedTagId = tagId;
				Refresh();
			});
		_tagButtons.AddChild(button);
	}

	private InventoryItemBox CreateItemBox(InventoryEntry entry)
	{
		if (InventoryItemBoxScene is null)
		{
			throw new InvalidOperationException("InventoryItemBoxScene is not assigned.");
		}

		var instance = InventoryItemBoxScene.Instantiate();
		if (instance is not InventoryItemBox itemBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("InventoryItemBox scene root must be InventoryItemBox.");
		}

		itemBox.Setup(entry);
		itemBox.EntrySelected += OnEntrySelected;
		return itemBox;
	}

	private void OnEntrySelected(InventoryEntry entry)
	{
		var analysis = Game.ItemUseService.Analyze(entry);
		var action = new DetailPanelAction(
			analysis.IsSupported ? ResolveEntryActionLabel(entry) : "不可使用",
			analysis.IsSupported,
			() =>
			{
				ShowTargetSelectionPanel(entry);
				return Task.CompletedTask;
			});
		UIRoot.Instance.ShowInventoryEntryDetailPanel(entry, action);
	}

	private void ShowTargetSelectionPanel(InventoryEntry entry)
	{
		if (ItemTargetSelectionPanelScene is null)
		{
			throw new InvalidOperationException("ItemTargetSelectionPanelScene is not assigned.");
		}

		var instance = ItemTargetSelectionPanelScene.Instantiate();
		if (instance is not ItemTargetSelectionPanel panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException("ItemTargetSelectionPanel scene root must be ItemTargetSelectionPanel.");
		}

		panel.Configure(entry);
		UIRoot.Instance.ModalLayer.AddChild(panel);
	}

	private static string ResolveEntryActionLabel(InventoryEntry entry) =>
		entry.Definition.Type == ItemType.Equipment ? "装备" : "使用";

	private void UpdateCategoryButtons()
	{
		foreach (var category in Categories)
		{
			var button = _buttonsByCategoryKey[category.Key];
			var isSelected = category.Key == _selectedCategory.Key;
			button.Disabled = isSelected && _selectedTagId is null;
			button.Modulate = isSelected
				? new Color(1.0f, 0.92f, 0.68f)
				: Colors.White;
		}
	}

	private void OnInventoryChanged(InventoryChangedEvent _) => Refresh();

	private void OnSaveLoaded(SaveLoadedEvent _) => Refresh();

	private void ClearGrid()
	{
		ClearChildren(_gridContainer);
	}

	private static void ClearChildren(Node parent)
	{
		foreach (var child in parent.GetChildren())
		{
			parent.RemoveChild(child);
			child.QueueFree();
		}
	}
}
