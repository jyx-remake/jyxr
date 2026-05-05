using Game.Application;
using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class ShopPanel : JyPanel
{
	[Export]
	public PackedScene ShopItemBoxScene { get; set; } = null!;

	private static readonly ShopCategory[] Categories =
	[
		new("All", "全部", null),
		new("Equipment", "装备", ItemType.Equipment),
		new("Consumable", "消耗品", ItemType.Consumable),
		new("SkillBook", "武学书", ItemType.SkillBook),
		new("SpecialSkillBook", "特技书", ItemType.SpecialSkillBook),
		new("TalentBook", "天赋书", ItemType.TalentBook),
		new("QuestItem", "剧情物品", ItemType.QuestItem),
		new("Booster", "强化道具", ItemType.Booster),
		new("Utility", "功能道具", ItemType.Utility),
	];

	private readonly List<IDisposable> _subscriptions = [];
	private readonly Dictionary<string, Button> _buttonsByCategoryKey = [];
	private string _shopId = string.Empty;
	private ShopView? _shop;
	private ShopMode _mode = ShopMode.Buy;
	private ShopCategory _selectedCategory = Categories[0];

	private TextureRect _backgroundTexture = null!;
	private Label _titleLabel = null!;
	private Button _buyModeButton = null!;
	private Button _sellModeButton = null!;
	private Button _leaveButton = null!;
	private CheckBox _quickBuyCheckBox = null!;
	private Label _categoryLabel = null!;
	private Label _countLabel = null!;
	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;
	private Label _promptLabel = null!;
	private Label _silverLabel = null!;
	private Label _goldLabel = null!;

	public override void _Ready()
	{
		base._Ready();
		_backgroundTexture = GetNode<TextureRect>("%BackgroundTexture");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_buyModeButton = GetNode<Button>("%BuyModeButton");
		_sellModeButton = GetNode<Button>("%SellModeButton");
		_leaveButton = GetNode<Button>("%LeaveButton");
		_quickBuyCheckBox = GetNode<CheckBox>("%QuickBuyCheckBox");
		_categoryLabel = GetNode<Label>("%CategoryLabel");
		_countLabel = GetNode<Label>("%CountLabel");
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_promptLabel = GetNode<Label>("%PromptLabel");
		_silverLabel = GetNode<Label>("%SilverLabel");
		_goldLabel = GetNode<Label>("%GoldLabel");

		foreach (var category in Categories)
		{
			var button = GetNode<Button>($"%{category.Key}Button");
			_buttonsByCategoryKey.Add(category.Key, button);
			button.Pressed += () => SelectCategory(category);
		}

		_buyModeButton.Pressed += () => SelectMode(ShopMode.Buy);
		_sellModeButton.Pressed += () => SelectMode(ShopMode.Sell);
		_leaveButton.Pressed += QueueFree;
		_subscriptions.Add(Game.Session.Events.Subscribe<InventoryChangedEvent>(OnInventoryChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<SaveLoadedEvent>(OnSaveLoaded));

		if (!string.IsNullOrWhiteSpace(_shopId))
		{
			LoadShop();
		}
	}

	public override void _ExitTree()
	{
		foreach (var subscription in _subscriptions)
		{
			subscription.Dispose();
		}

		_subscriptions.Clear();
	}

	public void Configure(string shopId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
		_shopId = shopId;
		if (IsInsideTree())
		{
			LoadShop();
		}
	}

	private void LoadShop()
	{
		_shop = Game.ShopService.Open(_shopId);
		_titleLabel.Text = _shop.Definition.Name;
		_backgroundTexture.Texture = AssetResolver.LoadTextureResource(_shop.Definition.Background);
		if (!string.IsNullOrWhiteSpace(_shop.Definition.Music))
		{
			Game.Audio.PlayBgm(_shop.Definition.Music);
		}

		Refresh();
	}

	private void SelectMode(ShopMode mode)
	{
		_mode = mode;
		Refresh();
	}

	private void SelectCategory(ShopCategory category)
	{
		_selectedCategory = category;
		Refresh();
	}

	private void Refresh()
	{
		ClearGrid();
		UpdateModeButtons();
		UpdateCategoryButtons();
		UpdateCurrencyLabels();

		if (_shop is null)
		{
			return;
		}

		_categoryLabel.Text = _selectedCategory.DisplayName;
		_promptLabel.Text = _mode == ShopMode.Buy
			? "客官需要买点什么？"
			: "有什么宝贝要出手？";

		if (_mode == ShopMode.Buy)
		{
			RefreshProducts();
			return;
		}

		RefreshInventoryEntries();
	}

	private void RefreshProducts()
	{
		var products = Game.ShopService.Open(_shopId).Products
			.Where(product => MatchesSelectedCategory(product.ItemType))
			.ToList();
		_countLabel.Text = $"{products.Count} 项";
		_emptyLabel.Visible = products.Count == 0;

		foreach (var product in products)
		{
			var itemBox = CreateItemBox();
			itemBox.SetupProduct(product, CanBuy(product));
			itemBox.ProductSelected += OnProductSelected;
			_gridContainer.AddChild(itemBox);
		}
	}

	private void RefreshInventoryEntries()
	{
		var entries = Game.State.Inventory.Entries
			.Where(entry => MatchesSelectedCategory(entry.Definition.Type))
			.OrderBy(entry => entry.EntryNumber)
			.ToList();
		_countLabel.Text = $"{entries.Count} 项";
		_emptyLabel.Visible = entries.Count == 0;

		foreach (var entry in entries)
		{
			var sellPrice = Game.ShopService.GetSellPrice(entry.Definition);
			var itemBox = CreateItemBox();
			itemBox.SetupInventoryEntry(entry, sellPrice, entry.Definition.CanDrop && sellPrice > 0);
			itemBox.InventoryEntrySelected += OnInventoryEntrySelected;
			_gridContainer.AddChild(itemBox);
		}
	}

	private ShopItemBox CreateItemBox()
	{
		if (ShopItemBoxScene is null)
		{
			throw new InvalidOperationException("ShopItemBoxScene is not assigned.");
		}

		var instance = ShopItemBoxScene.Instantiate();
		if (instance is not ShopItemBox itemBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("ShopItemBox scene root must be ShopItemBox.");
		}

		return itemBox;
	}

	private async void OnProductSelected(ShopProductView product)
	{
		try
		{
			if (!_quickBuyCheckBox.ButtonPressed)
			{
				var confirmed = await UIRoot.Instance.ShowConfirmAsync($"花费 {FormatProductPrice(product)} 购买【{product.DisplayName}】？");
				if (!confirmed)
				{
					return;
				}
			}

			var result = Game.ShopService.Buy(_shopId, product.ProductIndex);
			HandleTransactionResult(result, showToast: false);
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Buying shop product failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private async void OnInventoryEntrySelected(InventoryEntry entry)
	{
		try
		{
			var sellPrice = Game.ShopService.GetSellPrice(entry.Definition);
			var confirmed = await UIRoot.Instance.ShowConfirmAsync($"卖出【{entry.Definition.Name}】获得 {sellPrice} 银两？");
			if (!confirmed)
			{
				return;
			}

			var result = Game.ShopService.Sell(entry);
			HandleTransactionResult(result, showToast: true);
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Selling inventory entry failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private void HandleTransactionResult(ShopTransactionResult result, bool showToast)
	{
		if (result.Success)
		{
			if (showToast)
			{
				UIRoot.Instance.ShowToast(result.Message);
			}

			Refresh();
			return;
		}

		UIRoot.Instance.ShowSuggestion(result.Message);
	}

	private bool CanBuy(ShopProductView product)
	{
		if (product.RemainingLimit == 0)
		{
			return false;
		}

		var price = product.GetUnitPrice(product.DefaultCurrencyKind);
		return price is not null && product.DefaultCurrencyKind switch
		{
			ShopCurrencyKind.Silver => Game.State.Currency.CanSpendSilver(price.Value),
			ShopCurrencyKind.Gold => Game.State.Currency.CanSpendGold(price.Value),
			_ => false,
		};
	}

	private bool MatchesSelectedCategory(ItemType itemType) =>
		_selectedCategory.ItemType is null || itemType == _selectedCategory.ItemType.Value;

	private void UpdateModeButtons()
	{
		_buyModeButton.Disabled = _mode == ShopMode.Buy;
		_sellModeButton.Disabled = _mode == ShopMode.Sell;
		_buyModeButton.Modulate = _mode == ShopMode.Buy ? new Color(1.0f, 0.92f, 0.68f) : Colors.White;
		_sellModeButton.Modulate = _mode == ShopMode.Sell ? new Color(1.0f, 0.92f, 0.68f) : Colors.White;
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

	private void UpdateCurrencyLabels()
	{
		_silverLabel.Text = Game.State.Currency.Silver.ToString();
		_goldLabel.Text = Game.State.Currency.Gold.ToString();
	}

	private void OnInventoryChanged(InventoryChangedEvent _) => Refresh();

	private void OnCurrencyChanged(CurrencyChangedEvent _) => Refresh();

	private void OnSaveLoaded(SaveLoadedEvent _) => Refresh();

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private static string FormatProductPrice(ShopProductView product)
	{
		if (product.Price is not null)
		{
			return $"{product.Price.Value} 银两";
		}

		if (product.PremiumPrice is not null)
		{
			return $"{product.PremiumPrice.Value} 元宝";
		}

		return "0 银两";
	}

	private enum ShopMode
	{
		Buy,
		Sell,
	}

	private sealed record ShopCategory(string Key, string DisplayName, ItemType? ItemType);
}
