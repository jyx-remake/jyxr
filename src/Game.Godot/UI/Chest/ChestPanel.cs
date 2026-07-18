using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Presentation.Items;
using Godot;

namespace Game.Godot.UI;

public partial class ChestPanel : JyPanel
{
	[Export]
	public PackedScene ItemBoxScene { get; set; } = null!;
	[Export]
	public PackedScene TagButtonScene { get; set; } = null!;

	private static readonly IReadOnlyList<ItemCategoryOption> Categories = ItemCatalogPresentation.Categories
		.Where(category => category.ItemType != ItemType.QuestItem)
		.ToList();

	private readonly Dictionary<string, Button> _buttonsByCategoryKey = [];
	private ChestMode _mode = ChestMode.Deposit;
	private ItemCategoryOption _selectedCategory = Categories[0];
	private string? _selectedTagId;

	private Button _depositModeButton = null!;
	private Button _withdrawModeButton = null!;
	private Button _leaveButton = null!;
	private CheckBox _quickTransferCheckBox = null!;
	private Label _titleLabel = null!;
	private Label _countLabel = null!;
	private Label _promptLabel = null!;
	private Label _capacityLabel = null!;
	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;
	private HFlowContainer _tagButtons = null!;
	private IDisposable? _saveLoadedSubscription;

	public override void _Ready()
	{
		base._Ready();
		_depositModeButton = GetNode<Button>("%DepositModeButton");
		_withdrawModeButton = GetNode<Button>("%WithdrawModeButton");
		_leaveButton = GetNode<Button>("%LeaveButton");
		_quickTransferCheckBox = GetNode<CheckBox>("%QuickTransferCheckBox");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_countLabel = GetNode<Label>("%CountLabel");
		_promptLabel = GetNode<Label>("%PromptLabel");
		_capacityLabel = GetNode<Label>("%CapacityLabel");
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_tagButtons = GetNode<HFlowContainer>("%TagButtons");

		InitializeCategoryButtons();

		_depositModeButton.Pressed += () => SelectMode(ChestMode.Deposit);
		_withdrawModeButton.Pressed += () => SelectMode(ChestMode.Withdraw);
		_leaveButton.Pressed += QueueFree;
		_saveLoadedSubscription = Game.Session.Events.Subscribe<SaveLoadedEvent>(_ => QueueFree());

		Refresh();
	}

	public override void _ExitTree()
	{
		_saveLoadedSubscription?.Dispose();
		_saveLoadedSubscription = null;
		base._ExitTree();
	}

	private void SelectMode(ChestMode mode)
	{
		_mode = mode;
		Refresh();
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
			button.CustomMinimumSize = new Vector2(200, category.ItemType is null ? 62 : 56);
			button.AddThemeFontSizeOverride("font_size", category.ItemType is null ? 42 : 30);
			button.Pressed += () => SelectCategory(category);
			container.AddChild(button);
			_buttonsByCategoryKey.Add(category.Key, button);
		}
	}

	private void Refresh()
	{
		ClearGrid();
		UpdateModeButtons();

		var chest = Game.ChestService.Open();
		var sourceEntries = GetEntriesForCurrentMode();
		var tags = ResolveAvailableTags(sourceEntries.Select(entry => entry.Definition));
		UpdateCategoryButtons();
		UpdateTagButtons(tags);
		var entries = sourceEntries
			.Where(EntryMatchesSelectedCategory)
			.OrderBy(entry => entry.EntryNumber)
			.ToList();

		_titleLabel.Text = "储物箱";
		_countLabel.Text = $"{entries.Count} 项";
		_promptLabel.Text = _mode == ChestMode.Deposit
			? "请问您存入什么物品？"
			: "请问您取出什么物品？";
		_capacityLabel.Text = $"当前总数 {chest.StoredItemCount}/{chest.Capacity}";
		_emptyLabel.Visible = entries.Count == 0;

		foreach (var entry in entries)
		{
			var itemBox = CreateItemBox();
			itemBox.SetupTransferEntry(
				entry,
				_mode == ChestMode.Deposit ? "存入" : "取出",
				CanSelectEntry(entry, chest));
			itemBox.InventoryEntrySelected += OnInventoryEntrySelected;
			_gridContainer.AddChild(itemBox);
		}
	}

	private IReadOnlyList<InventoryEntry> GetEntriesForCurrentMode() =>
		_mode == ChestMode.Deposit
			? Game.State.Inventory.Entries
			: Game.State.Chest.Inventory.Entries;

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

	private bool CanSelectEntry(InventoryEntry entry, ChestView chest)
	{
		if (_mode == ChestMode.Withdraw)
		{
			return true;
		}

		return Game.ChestService.CanStore(entry.Definition) && chest.StoredItemCount < chest.Capacity;
	}

	private ShopItemBox CreateItemBox()
	{
		if (ItemBoxScene is null)
		{
			throw new InvalidOperationException("ItemBoxScene is not assigned.");
		}

		var instance = ItemBoxScene.Instantiate();
		if (instance is not ShopItemBox itemBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Chest item box scene root must be ShopItemBox.");
		}

		return itemBox;
	}

	private void OnInventoryEntrySelected(InventoryEntry entry)
	{
		var chest = Game.ChestService.Open();
		var canTransfer = CanSelectEntry(entry, chest);
		if (_quickTransferCheckBox.ButtonPressed)
		{
			if (!canTransfer)
			{
				UIRoot.Instance.ShowSuggestion("当前不可存取。");
				return;
			}

			TransferEntry(entry);
			return;
		}

		var actionLabel = _mode == ChestMode.Deposit ? "存入" : "取出";
		UIRoot.Instance.ShowInventoryEntryDetailPanel(
			entry,
			new DetailPanelAction(
				canTransfer ? actionLabel : "不可存入",
				canTransfer,
				() =>
				{
					TransferEntry(entry);
					return Task.CompletedTask;
				}));
	}

	private void TransferEntry(InventoryEntry entry)
	{
		try
		{
			var result = _mode == ChestMode.Deposit
				? Game.ChestService.Deposit(entry)
				: Game.ChestService.Withdraw(entry);

			if (result.Success)
			{
				UIRoot.Instance.ShowToast(result.Message);
				Refresh();
				return;
			}

			UIRoot.Instance.ShowSuggestion(result.Message);
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Chest transfer failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private void UpdateModeButtons()
	{
		_depositModeButton.Disabled = _mode == ChestMode.Deposit;
		_withdrawModeButton.Disabled = _mode == ChestMode.Withdraw;
		_depositModeButton.Modulate = _mode == ChestMode.Deposit ? new Color(1.0f, 0.92f, 0.68f) : Colors.White;
		_withdrawModeButton.Modulate = _mode == ChestMode.Withdraw ? new Color(1.0f, 0.92f, 0.68f) : Colors.White;
	}

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

	private enum ChestMode
	{
		Deposit,
		Withdraw,
	}

}
