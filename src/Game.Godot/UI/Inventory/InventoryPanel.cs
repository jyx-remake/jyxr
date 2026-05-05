using Game.Application;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class InventoryPanel : JyPanel
{
	[Export]
	public PackedScene InventoryItemBoxScene { get; set; } = null!;

	[Export]
	public PackedScene ItemTargetSelectionPanelScene { get; set; } = null!;

	private static readonly InventoryCategory[] Categories =
	[
		new("All", "全部", null),
		new("Equipment", "装备", ItemType.Equipment),
		new("Consumable", "消耗品", ItemType.Consumable),
		new("SkillBook", "武学书", ItemType.SkillBook),
		new("SpecialSkillBook", "绝技书", ItemType.SpecialSkillBook),
		new("TalentBook", "天赋书", ItemType.TalentBook),
		new("QuestItem", "剧情物品", ItemType.QuestItem),
		new("Booster", "强化道具", ItemType.Booster),
		new("Utility", "功能道具", ItemType.Utility),
	];

	private readonly List<IDisposable> _subscriptions = [];
	private readonly Dictionary<string, Button> _buttonsByCategoryKey = [];

	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;
	private Label _categoryLabel = null!;
	private Label _countLabel = null!;
	private InventoryCategory _selectedCategory = Categories[0];

	public override void _Ready()
	{
		base._Ready();
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_categoryLabel = GetNode<Label>("%CategoryLabel");
		_countLabel = GetNode<Label>("%CountLabel");

		foreach (var category in Categories)
		{
			var button = GetNode<Button>($"%{category.Key}Button");
			_buttonsByCategoryKey.Add(category.Key, button);
			button.Pressed += () => SelectCategory(category);
		}

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

	private void SelectCategory(InventoryCategory category)
	{
		_selectedCategory = category;
		Refresh();
	}

	private void Refresh()
	{
		ClearGrid();
		UpdateCategoryButtons();

		var entries = Game.State.Inventory.Entries
			.Where(EntryMatchesSelectedCategory)
			.OrderBy(entry => entry.EntryNumber)
			.ToList();

		_categoryLabel.Text = _selectedCategory.DisplayName;
		_countLabel.Text = $"{entries.Count} 项";
		_emptyLabel.Visible = entries.Count == 0;

		foreach (var entry in entries)
		{
			var itemBox = CreateItemBox(entry);
			_gridContainer.AddChild(itemBox);
		}
	}

	private bool EntryMatchesSelectedCategory(InventoryEntry entry) =>
		_selectedCategory.ItemType is null || entry.Definition.Type == _selectedCategory.ItemType.Value;

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
		if (!analysis.IsSupported)
		{
			UIRoot.Instance.ShowSuggestion(analysis.Message);
			return;
		}

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

	private void UpdateCategoryButtons()
	{
		foreach (var category in Categories)
		{
			var button = _buttonsByCategoryKey[category.Key];
			var isSelected = category.Key == _selectedCategory.Key;
			button.Disabled = isSelected;
			button.Modulate = isSelected
				? new Color(1.0f, 0.92f, 0.68f)
				: Colors.White;
		}
	}

	private void OnInventoryChanged(InventoryChangedEvent _) => Refresh();

	private void OnSaveLoaded(SaveLoadedEvent _) => Refresh();

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private sealed record InventoryCategory(string Key, string DisplayName, ItemType? ItemType);
}
