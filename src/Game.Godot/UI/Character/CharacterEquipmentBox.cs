using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterEquipmentBox : Control
{
	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	public event Action? EquipmentPrimaryClicked;
	public event Action? EquipmentSecondaryClicked;

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;

	private EquipmentInstance? _equipment;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		GuiInput += OnGuiInput;
		Refresh();
	}

	public void Setup(EquipmentInstance equipment)
	{
		ArgumentNullException.ThrowIfNull(equipment);
		_equipment = equipment;
		TooltipText = equipment.Definition.Name;
		Refresh();
	}

	public override Control? _MakeCustomTooltip(string forText)
	{
		if (_equipment is null)
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
			throw new InvalidOperationException("Equipment tooltip scene root must be InventoryItemTooltip.");
		}

		tooltip.Setup(_equipment);
		return tooltip;
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _equipment is null)
		{
			return;
		}

		_avatar.Texture = AssetResolver.LoadTextureResource(_equipment.Definition.Picture);
		_nameLabel.Text = _equipment.Definition.Name;
	}

	private void OnGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton { Pressed: true } mouseButton)
		{
			return;
		}

		switch (mouseButton.ButtonIndex)
		{
			case MouseButton.Left:
				EquipmentPrimaryClicked?.Invoke();
				GetViewport().SetInputAsHandled();
				break;
			case MouseButton.Right:
				EquipmentSecondaryClicked?.Invoke();
				GetViewport().SetInputAsHandled();
				break;
		}
	}
}
