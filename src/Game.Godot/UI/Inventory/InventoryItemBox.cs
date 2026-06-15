using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class InventoryItemBox : TextureButton
{
	public const float DesignWidth = 180f;
	public const float DesignHeight = 180f;

	private static readonly Rect2 MaskDesignRect = new(new Vector2(30f, 30f), new Vector2(120f, 120f));
	private static readonly Rect2 RarityBandDesignRect = new(new Vector2(1f, 100f), new Vector2(176f, 84f));
	private static readonly Rect2 AvatarDesignRect = new(new Vector2(22f, 24f), new Vector2(131f, 131f));
	private static readonly Rect2 NameLabelDesignRect = new(new Vector2(-12f, 127f), new Vector2(204f, 50f));
	private static readonly Rect2 StackLabelDesignRect = new(new Vector2(116f, 7f), new Vector2(54f, 33f));
	private const int StackLabelDesignFontSize = 34;
	private const int StackLabelDesignOutlineSize = 5;
	private const float MinPresentationScale = 0.55f;
	private const float MaxPresentationScale = 1f;

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	public event Action<InventoryEntry>? EntrySelected;

	private TextureRect _mask = null!;
	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _stackLabel = null!;
	private Panel _rarityBand = null!;
	private StyleBoxFlat _rarityBandStyle = null!;
	private InventoryEntry? _entry;
	private float _presentationScale = 1f;

	public override void _Ready()
	{
		_mask = GetNode<TextureRect>("Mask");
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_stackLabel = GetNode<Label>("%StackLabel");
		_rarityBand = GetNode<Panel>("%RarityBand");
		_rarityBandStyle = DuplicateBandStyle(_rarityBand);
		DuplicateLabelSettings(_nameLabel);
		Pressed += OnPressed;
		ApplyPresentationScale();
		Refresh();
	}

	public void SetPresentationScale(float scale)
	{
		_presentationScale = Mathf.Clamp(scale, MinPresentationScale, MaxPresentationScale);
		ApplyPresentationScale();
	}

	public void Setup(InventoryEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry);
		_entry = entry;
		TooltipText = entry.Definition.Name;
		Refresh();
	}

	public override Control? _MakeCustomTooltip(string forText)
	{
		if (_entry is null)
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
			throw new InvalidOperationException("InventoryItemTooltip scene root must be InventoryItemTooltip.");
		}

		tooltip.Setup(_entry);
		return tooltip;
	}

	private void OnPressed()
	{
		if (_entry is not null)
		{
			EntrySelected?.Invoke(_entry);
		}
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _entry is null)
		{
			return;
		}

		_avatar.Texture = AssetResolver.LoadTextureResource(_entry.Definition.Picture);
		_nameLabel.Text = _entry.Definition.Name;
		_rarityBandStyle.BgColor = ItemRarityBandColorResolver.Resolve(_entry);
		var quantity = _entry is StackInventoryEntry stack ? stack.Quantity : 1;
		_stackLabel.Text = quantity > 1 ? quantity.ToString() : string.Empty;
	}

	private static StyleBoxFlat DuplicateBandStyle(Panel band)
	{
		var style = band.GetThemeStylebox("panel") as StyleBoxFlat;
		var duplicate = style?.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();
		band.AddThemeStyleboxOverride("panel", duplicate);
		return duplicate;
	}

	private void ApplyPresentationScale()
	{
		var size = new Vector2(DesignWidth, DesignHeight) * _presentationScale;
		CustomMinimumSize = size;
		Size = size;

		if (!IsInsideTree())
		{
			return;
		}

		_mask.Position = MaskDesignRect.Position * _presentationScale;
		_mask.Size = MaskDesignRect.Size * _presentationScale;
		_rarityBand.Position = RarityBandDesignRect.Position * _presentationScale;
		_rarityBand.Size = RarityBandDesignRect.Size * _presentationScale;
		_avatar.Position = AvatarDesignRect.Position * _presentationScale;
		_avatar.Size = AvatarDesignRect.Size * _presentationScale;
		_nameLabel.Position = NameLabelDesignRect.Position * _presentationScale;
		_nameLabel.Size = NameLabelDesignRect.Size * _presentationScale;
		_stackLabel.Position = StackLabelDesignRect.Position * _presentationScale;
		_stackLabel.Size = StackLabelDesignRect.Size * _presentationScale;

		var stackFontSize = Math.Max(18, (int)MathF.Round(StackLabelDesignFontSize * _presentationScale));
		var stackOutlineSize = Math.Max(1, (int)MathF.Round(StackLabelDesignOutlineSize * _presentationScale));
		_stackLabel.AddThemeFontSizeOverride("font_size", stackFontSize);
		_stackLabel.AddThemeConstantOverride("outline_size", stackOutlineSize);

		if (_nameLabel.LabelSettings is not null)
		{
			_nameLabel.LabelSettings.FontSize = Math.Max(16, (int)MathF.Round(26f * _presentationScale));
			_nameLabel.LabelSettings.OutlineSize = Math.Max(1, (int)MathF.Round(3f * _presentationScale));
		}
	}

	private static void DuplicateLabelSettings(Label label)
	{
		if (label.LabelSettings is not null)
		{
			label.LabelSettings = (LabelSettings)label.LabelSettings.Duplicate();
		}
	}
}
