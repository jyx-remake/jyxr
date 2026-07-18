using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Godot.Assets;
using Game.Presentation.Items;
using Godot;

namespace Game.Godot.UI;

public partial class ShopPanel : JyPanel
{
	[Export]
	public PackedScene ShopItemBoxScene { get; set; } = null!;
	[Export]
	public PackedScene TagButtonScene { get; set; } = null!;

	private static readonly IReadOnlyList<ItemCategoryOption> Categories = ItemCatalogPresentation.Categories;

	private readonly Dictionary<string, Button> _buttonsByCategoryKey = [];
	private string _shopId = string.Empty;
	private ShopView? _shop;
	private ShopMode _mode = ShopMode.Buy;
	private ItemCategoryOption _selectedCategory = Categories[0];
	private string? _selectedTagId;

	private TextureRect _backgroundTexture = null!;
	private Label _titleLabel = null!;
	private Button _buyModeButton = null!;
	private Button _sellModeButton = null!;
	private Button _leaveButton = null!;
	private CheckBox _quickBuyCheckBox = null!;
	private CheckBox _quickSellCheckBox = null!;
	private Label _countLabel = null!;
	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;
	private Label _promptLabel = null!;
	private Label _silverLabel = null!;
	private Label _goldLabel = null!;
	private HFlowContainer _tagButtons = null!;
	private IDisposable? _saveLoadedSubscription;

	public override void _Ready()
	{
		base._Ready();
		_backgroundTexture = GetNode<TextureRect>("%BackgroundTexture");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_buyModeButton = GetNode<Button>("%BuyModeButton");
		_sellModeButton = GetNode<Button>("%SellModeButton");
		_leaveButton = GetNode<Button>("%LeaveButton");
		_quickBuyCheckBox = GetNode<CheckBox>("%QuickBuyCheckBox");
		_quickSellCheckBox = GetNode<CheckBox>("%QuickSellCheckBox");
		_countLabel = GetNode<Label>("%CountLabel");
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_promptLabel = GetNode<Label>("%PromptLabel");
		_silverLabel = GetNode<Label>("%SilverLabel");
		_goldLabel = GetNode<Label>("%GoldLabel");
		_tagButtons = GetNode<HFlowContainer>("%TagButtons");

		InitializeCategoryButtons();

		_buyModeButton.Pressed += () => SelectMode(ShopMode.Buy);
		_sellModeButton.Pressed += () => SelectMode(ShopMode.Sell);
		_leaveButton.Pressed += QueueFree;
		_saveLoadedSubscription = Game.Session.Events.Subscribe<SaveLoadedEvent>(_ => QueueFree());

		if (!string.IsNullOrWhiteSpace(_shopId))
		{
			LoadShop();
		}
	}

	public override void _ExitTree()
	{
		_saveLoadedSubscription?.Dispose();
		_saveLoadedSubscription = null;
		base._ExitTree();
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
		UpdateCurrencyLabels();

		if (_shop is null)
		{
			UpdateCategoryButtons();
			return;
		}

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
		var sourceProducts = Game.ShopService.Open(_shopId).Products;
		var tags = ResolveAvailableTags(sourceProducts.Select(product => product.Item));
		UpdateCategoryButtons();
		UpdateTagButtons(tags);
		var products = sourceProducts
			.Where(product => MatchesSelectedCategory(product.Item))
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
		var sourceEntries = Game.State.Inventory.Entries;
		var tags = ResolveAvailableTags(sourceEntries.Select(entry => entry.Definition));
		UpdateCategoryButtons();
		UpdateTagButtons(tags);
		var entries = sourceEntries
			.Where(entry => MatchesSelectedCategory(entry.Definition))
			.OrderBy(entry => entry.EntryNumber)
			.ToList();
		_countLabel.Text = $"{entries.Count} 项";
		_emptyLabel.Visible = entries.Count == 0;

		foreach (var entry in entries)
		{
			var sellPrice = Game.ShopService.GetSellPrice(entry.Definition);
			var itemBox = CreateItemBox();
			itemBox.SetupInventoryEntry(entry, sellPrice, Game.ShopService.CanSell(entry.Definition));
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

	private void OnProductSelected(ShopProductView product)
	{
		var canBuy = CanBuy(product);
		if (_quickBuyCheckBox.ButtonPressed)
		{
			if (!canBuy)
			{
				UIRoot.Instance.ShowSuggestion("当前不可购买。");
				return;
			}

			_ = BuyProductAsync(product);
			return;
		}

		UIRoot.Instance.ShowShopProductDetailPanel(
			product,
			new DetailPanelAction(
				canBuy ? "购买" : "不可购买",
				canBuy,
				() => BuyProductAsync(product)));
	}

	private async Task BuyProductAsync(ShopProductView product)
	{
		try
		{
			var result = Game.ShopService.Buy(_shopId, product.ProductIndex);
			HandleTransactionResult(result, showToast: false);
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Buying shop product failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private void OnInventoryEntrySelected(InventoryEntry entry)
	{
		var sellPrice = Game.ShopService.GetSellPrice(entry.Definition);
		var canSell = Game.ShopService.CanSell(entry.Definition);
		if (_quickSellCheckBox.ButtonPressed)
		{
			if (!canSell)
			{
				UIRoot.Instance.ShowSuggestion("当前不可卖。");
				return;
			}

			_ = SellInventoryEntryAsync(entry);
			return;
		}

		UIRoot.Instance.ShowInventoryEntryDetailPanel(
			entry,
			new DetailPanelAction(
				canSell ? $"卖出 {sellPrice}" : "不可卖",
				canSell,
				() => SellInventoryEntryAsync(entry)));
	}

	private async Task SellInventoryEntryAsync(InventoryEntry entry)
	{
		try
		{
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
			ShopCurrencyKind.Gold => Game.ProfileService.CanSpendYuanbao(price.Value),
			_ => false,
		};
	}

	private bool MatchesSelectedCategory(ItemDefinition item) =>
		ItemCatalogPresentation.Matches(item, _selectedCategory.ItemType, _selectedTagId);

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
			button.Disabled = isSelected && _selectedTagId is null;
			button.Modulate = isSelected
				? new Color(1.0f, 0.92f, 0.68f)
				: Colors.White;
		}
	}

	private void UpdateCurrencyLabels()
	{
		_silverLabel.Text = Game.State.Currency.Silver.ToString();
		_goldLabel.Text = Game.Profile.Yuanbao.ToString();
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

}
