using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class ShopItemBox : TextureButton
{
	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	public event Action<ShopProductView>? ProductSelected;
	public event Action<InventoryEntry>? InventoryEntrySelected;

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _priceLabel = null!;
	private Label _limitLabel = null!;
	private Panel _rarityBand = null!;
	private StyleBoxFlat _rarityBandStyle = null!;
	private ItemDefinition? _item;
	private EquipmentInstance? _equipment;
	private ShopProductView? _product;
	private InventoryEntry? _entry;
	private string _entryFooterText = string.Empty;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_priceLabel = GetNode<Label>("%PriceLabel");
		_limitLabel = GetNode<Label>("%LimitLabel");
		_rarityBand = GetNode<Panel>("%RarityBand");
		_rarityBandStyle = DuplicateBandStyle(_rarityBand);
		Pressed += OnPressed;
		Refresh();
	}

	public void SetupProduct(ShopProductView product, bool canBuy)
	{
		ArgumentNullException.ThrowIfNull(product);
		_product = product;
		_entry = null;
		_item = product.Item;
		_equipment = null;
		_entryFooterText = string.Empty;
		Disabled = !canBuy;
		TooltipText = product.DisplayName;
		Refresh();
	}

	public void SetupInventoryEntry(InventoryEntry entry, int sellPrice, bool canSell)
	{
		SetupEntry(entry, sellPrice is > 0 ? $"卖 {sellPrice}" : "不可卖", canSell);
	}

	public void SetupTransferEntry(InventoryEntry entry, string footerText, bool canSelect)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(footerText);
		SetupEntry(entry, footerText, canSelect);
	}

	private void SetupEntry(InventoryEntry entry, string footerText, bool canSelect)
	{
		ArgumentNullException.ThrowIfNull(entry);
		_product = null;
		_entry = entry;
		_item = entry.Definition;
		_equipment = entry is EquipmentInstanceInventoryEntry equipmentEntry
			? equipmentEntry.Equipment
			: null;
		_entryFooterText = footerText;
		Disabled = !canSelect;
		TooltipText = entry.Definition.Name;
		Refresh();
	}

	public override Control? _MakeCustomTooltip(string forText)
	{
		if (_item is null)
		{
			return null;
		}

		if (TooltipScene is null)
		{
			throw new InvalidOperationException("TooltipScene is not assigned.");
		}

		var instance = TooltipScene.Instantiate();
		if (instance is not InventoryItemTooltip tooltip)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Shop item tooltip scene root must be InventoryItemTooltip.");
		}

		if (_equipment is not null)
		{
			tooltip.Setup(_equipment);
			return tooltip;
		}

		tooltip.Setup(_item);
		return tooltip;
	}

	private void OnPressed()
	{
		if (_product is not null)
		{
			ProductSelected?.Invoke(_product);
			return;
		}

		if (_entry is not null)
		{
			InventoryEntrySelected?.Invoke(_entry);
		}
	}

	private void Refresh()
	{
		if (!IsInsideTree() || (_item is null && _product is null))
		{
			return;
		}

		_avatar.Texture = AssetResolver.LoadTextureResource(_product?.Picture ?? _item?.Picture);
		_nameLabel.Text = _product?.DisplayName ?? _item!.Name;
		_rarityBandStyle.BgColor = ResolveBandColor();

		if (_product is not null)
		{
			_priceLabel.Text = FormatProductPrice(_product);
			_limitLabel.Text = _product.RemainingLimit is null
				? string.Empty
				: $"余 {_product.RemainingLimit.Value}";
			return;
		}

		_priceLabel.Text = _entryFooterText;
		_limitLabel.Text = _entry is StackInventoryEntry stack && stack.Quantity > 1
			? $"x{stack.Quantity}"
			: string.Empty;
	}

	private Color ResolveBandColor()
	{
		if (_equipment is not null)
		{
			return ItemRarityBandColorResolver.Resolve(_equipment);
		}

		return ItemRarityBandColorResolver.Resolve(_item!);
	}

	private static StyleBoxFlat DuplicateBandStyle(Panel band)
	{
		var style = band.GetThemeStylebox("panel") as StyleBoxFlat;
		var duplicate = style?.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();
		band.AddThemeStyleboxOverride("panel", duplicate);
		return duplicate;
	}

	private static string FormatProductPrice(ShopProductView product)
	{
		if (product.Price is not null)
		{
			return $"银 {product.Price.Value}";
		}

		if (product.PremiumPrice is not null)
		{
			return $"元宝 {product.PremiumPrice.Value}";
		}

		return "无价";
	}
}
