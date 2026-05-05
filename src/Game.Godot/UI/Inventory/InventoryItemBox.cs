using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class InventoryItemBox : TextureButton
{
	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	public event Action<InventoryEntry>? EntrySelected;

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _stackLabel = null!;
	private InventoryEntry? _entry;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_stackLabel = GetNode<Label>("%StackLabel");
		Pressed += OnPressed;
		Refresh();
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
		var quantity = _entry is StackInventoryEntry stack ? stack.Quantity : 1;
		_stackLabel.Text = quantity > 1 ? quantity.ToString() : string.Empty;
	}
}
