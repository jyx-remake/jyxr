using Game.Application;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class ChestPanel : JyPanel
{
	[Export]
	public PackedScene ItemBoxScene { get; set; } = null!;

	private static readonly ChestCategory[] Categories =
	[
		new("All", "全部", null),
		new("Equipment", "装备", ItemType.Equipment),
		new("Consumable", "消耗品", ItemType.Consumable),
		new("SkillBook", "武学书", ItemType.SkillBook),
		new("SpecialSkillBook", "绝技书", ItemType.SpecialSkillBook),
		new("TalentBook", "天赋书", ItemType.TalentBook),
		new("Booster", "强化道具", ItemType.Booster),
		new("Utility", "功能道具", ItemType.Utility),
	];

	private readonly List<IDisposable> _subscriptions = [];
	private readonly Dictionary<string, Button> _buttonsByCategoryKey = [];
	private ChestMode _mode = ChestMode.Deposit;
	private ChestCategory _selectedCategory = Categories[0];

	private Button _depositModeButton = null!;
	private Button _withdrawModeButton = null!;
	private Button _leaveButton = null!;
	private Label _titleLabel = null!;
	private Label _categoryLabel = null!;
	private Label _countLabel = null!;
	private Label _promptLabel = null!;
	private Label _capacityLabel = null!;
	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;

	public override void _Ready()
	{
		base._Ready();
		_depositModeButton = GetNode<Button>("%DepositModeButton");
		_withdrawModeButton = GetNode<Button>("%WithdrawModeButton");
		_leaveButton = GetNode<Button>("%LeaveButton");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_categoryLabel = GetNode<Label>("%CategoryLabel");
		_countLabel = GetNode<Label>("%CountLabel");
		_promptLabel = GetNode<Label>("%PromptLabel");
		_capacityLabel = GetNode<Label>("%CapacityLabel");
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_emptyLabel = GetNode<Label>("%EmptyLabel");

		foreach (var category in Categories)
		{
			var button = GetNode<Button>($"%{category.Key}Button");
			_buttonsByCategoryKey.Add(category.Key, button);
			button.Pressed += () => SelectCategory(category);
		}

		_depositModeButton.Pressed += () => SelectMode(ChestMode.Deposit);
		_withdrawModeButton.Pressed += () => SelectMode(ChestMode.Withdraw);
		_leaveButton.Pressed += QueueFree;
		_subscriptions.Add(Game.Session.Events.Subscribe<InventoryChangedEvent>(OnInventoryChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<ChestChangedEvent>(OnChestChanged));
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

	private void SelectMode(ChestMode mode)
	{
		_mode = mode;
		Refresh();
	}

	private void SelectCategory(ChestCategory category)
	{
		_selectedCategory = category;
		Refresh();
	}

	private void Refresh()
	{
		ClearGrid();
		UpdateModeButtons();
		UpdateCategoryButtons();

		var chest = Game.ChestService.Open();
		var entries = GetEntriesForCurrentMode()
			.Where(EntryMatchesSelectedCategory)
			.OrderBy(entry => entry.EntryNumber)
			.ToList();

		_titleLabel.Text = "储物箱";
		_categoryLabel.Text = _selectedCategory.DisplayName;
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
		_selectedCategory.ItemType is null || entry.Definition.Type == _selectedCategory.ItemType.Value;

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
			button.Disabled = isSelected;
			button.Modulate = isSelected
				? new Color(1.0f, 0.92f, 0.68f)
				: Colors.White;
		}
	}

	private void OnInventoryChanged(InventoryChangedEvent _) => Refresh();

	private void OnChestChanged(ChestChangedEvent _) => Refresh();

	private void OnSaveLoaded(SaveLoadedEvent _) => Refresh();

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private enum ChestMode
	{
		Deposit,
		Withdraw,
	}

	private sealed record ChestCategory(string Key, string DisplayName, ItemType? ItemType);
}
