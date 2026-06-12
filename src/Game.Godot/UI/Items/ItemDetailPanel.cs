using Game.Application;
using Game.Application.Formatters;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class ItemDetailPanel : JyPanel
{
	public event Action? PrimaryActionPressed;

	private TextureRect _itemTexture = null!;
	private Label _nameLabel = null!;
	private Label _typeLabel = null!;
	private Label _metaLabel = null!;
	private RichTextLabel _contentLabel = null!;
	private TextureButton _primaryActionButton = null!;
	private Label _primaryActionLabel = null!;

	private ItemDefinition? _item;
	private EquipmentInstance? _equipment;
	private InventoryEntry? _entry;
	private ShopProductView? _product;
	private string _primaryActionText = string.Empty;
	private bool _primaryActionEnabled;

	public override void _Ready()
	{
		base._Ready();
		_itemTexture = GetNode<TextureRect>("%ItemTexture");
		_nameLabel = GetNode<Label>("%NameLabel");
		_typeLabel = GetNode<Label>("%TypeLabel");
		_metaLabel = GetNode<Label>("%MetaLabel");
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		_primaryActionButton = GetNode<TextureButton>("%PrimaryActionButton");
		_primaryActionLabel = GetNode<Label>("%PrimaryActionLabel");

		_primaryActionButton.Pressed += OnPrimaryActionPressed;
		Refresh();
	}

	public void Configure(InventoryEntry entry, string primaryActionText = "", bool primaryActionEnabled = false)
	{
		ArgumentNullException.ThrowIfNull(entry);
		_entry = entry;
		_product = null;
		_equipment = entry is EquipmentInstanceInventoryEntry equipmentEntry
			? equipmentEntry.Equipment
			: null;
		_item = entry.Definition;
		_primaryActionText = primaryActionText;
		_primaryActionEnabled = primaryActionEnabled;
		Refresh();
	}

	public void Configure(ShopProductView product, string primaryActionText = "", bool primaryActionEnabled = false)
	{
		ArgumentNullException.ThrowIfNull(product);
		_product = product;
		_entry = null;
		_equipment = null;
		_item = product.Item;
		_primaryActionText = primaryActionText;
		_primaryActionEnabled = primaryActionEnabled;
		Refresh();
	}

	public void Configure(EquipmentInstance equipment, string primaryActionText = "", bool primaryActionEnabled = false)
	{
		ArgumentNullException.ThrowIfNull(equipment);
		_equipment = equipment;
		_item = equipment.Definition;
		_entry = null;
		_product = null;
		_primaryActionText = primaryActionText;
		_primaryActionEnabled = primaryActionEnabled;
		Refresh();
	}

	public void Configure(ItemDefinition item, string primaryActionText = "", bool primaryActionEnabled = false)
	{
		ArgumentNullException.ThrowIfNull(item);
		_item = item;
		_equipment = null;
		_entry = null;
		_product = null;
		_primaryActionText = primaryActionText;
		_primaryActionEnabled = primaryActionEnabled;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _item is null)
		{
			return;
		}

		_itemTexture.Texture = AssetResolver.LoadTextureResource(_product?.Picture ?? _item.Picture);
		_nameLabel.Text = _product?.DisplayName ?? _item.Name;
		_typeLabel.Text = FormatItemType(_item.Type);
		_metaLabel.Text = BuildMetaText();
		_contentLabel.Text = _equipment is not null
			? ItemDescriptionFormatter.FormatBbCodeCn(_equipment, Game.ContentRepository)
			: ItemDescriptionFormatter.FormatBbCodeCn(_item, Game.ContentRepository);
		RefreshPrimaryAction();
	}

	private void RefreshPrimaryAction()
	{
		var visible = !string.IsNullOrWhiteSpace(_primaryActionText);
		_primaryActionButton.Visible = visible;
		_primaryActionButton.Disabled = !_primaryActionEnabled;
		_primaryActionButton.Modulate = _primaryActionEnabled
			? Colors.White
			: new Color(0.62f, 0.62f, 0.62f, 0.82f);
		_primaryActionLabel.Text = _primaryActionText;
	}

	private string BuildMetaText()
	{
		if (_product is not null)
		{
			var parts = new List<string>();
			if (_product.Price is not null)
			{
				parts.Add($"银两 {_product.Price.Value}");
			}

			if (_product.PremiumPrice is not null)
			{
				parts.Add($"元宝 {_product.PremiumPrice.Value}");
			}

			if (_product.RemainingLimit is not null)
			{
				parts.Add($"余 {_product.RemainingLimit.Value}");
			}

			return parts.Count == 0 ? "无价" : string.Join("   ", parts);
		}

		if (_entry is StackInventoryEntry stack)
		{
			return $"数量 {stack.Quantity}";
		}

		if (_equipment is not null)
		{
			return "独立装备";
		}

		return string.Empty;
	}

	private void OnPrimaryActionPressed()
	{
		if (!_primaryActionEnabled)
		{
			return;
		}

		PrimaryActionPressed?.Invoke();
		QueueFree();
	}

	private static string FormatItemType(ItemType itemType) =>
		itemType switch
		{
			ItemType.Consumable => "消耗品",
			ItemType.Equipment => "装备",
			ItemType.SkillBook => "武学书",
			ItemType.QuestItem => "剧情物品",
			ItemType.SpecialSkillBook => "绝技书",
			ItemType.TalentBook => "天赋书",
			ItemType.Booster => "强化道具",
			ItemType.Utility => "功能道具",
			_ => itemType.ToString(),
		};
}
