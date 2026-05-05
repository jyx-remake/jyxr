using Game.Application;
using Game.Core.Model;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterEquipmentTab : Control
{
	[Export]
	public PackedScene EquipmentSelectionPanelScene { get; set; } = null!;

	private JyButton _weaponSlot = null!;
	private JyButton _armorSlot = null!;
	private JyButton _accessorySlot = null!;
	private CharacterEquipmentBox _weaponBox = null!;
	private CharacterEquipmentBox _armorBox = null!;
	private CharacterEquipmentBox _accessoryBox = null!;
	private Label _weaponEmptyLabel = null!;
	private Label _armorEmptyLabel = null!;
	private Label _accessoryEmptyLabel = null!;

	private CharacterInstance? _character;

	public override void _Ready()
	{
		_weaponSlot = GetNode<JyButton>("%WeaponSlot");
		_armorSlot = GetNode<JyButton>("%ArmorSlot");
		_accessorySlot = GetNode<JyButton>("%AccessorySlot");
		_weaponBox = GetNode<CharacterEquipmentBox>("%WeaponBox");
		_armorBox = GetNode<CharacterEquipmentBox>("%ArmorBox");
		_accessoryBox = GetNode<CharacterEquipmentBox>("%AccessoryBox");
		_weaponEmptyLabel = GetNode<Label>("%WeaponEmptyLabel");
		_armorEmptyLabel = GetNode<Label>("%ArmorEmptyLabel");
		_accessoryEmptyLabel = GetNode<Label>("%AccessoryEmptyLabel");

		ConnectSlot(_weaponSlot, _weaponBox, EquipmentSlotType.Weapon);
		ConnectSlot(_armorSlot, _armorBox, EquipmentSlotType.Armor);
		ConnectSlot(_accessorySlot, _accessoryBox, EquipmentSlotType.Accessory);
		Refresh();
	}

	public void Setup(CharacterInstance character)
	{
		ArgumentNullException.ThrowIfNull(character);
		_character = character;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _character is null)
		{
			return;
		}

		SyncSlot(EquipmentSlotType.Weapon, _weaponBox, _weaponEmptyLabel);
		SyncSlot(EquipmentSlotType.Armor, _armorBox, _armorEmptyLabel);
		SyncSlot(EquipmentSlotType.Accessory, _accessoryBox, _accessoryEmptyLabel);
	}

	private void SyncSlot(
		EquipmentSlotType slotType,
		CharacterEquipmentBox box,
		Label emptyLabel)
	{
		var equipment = _character!.GetEquipment(slotType);
		if (equipment is null)
		{
			box.Visible = false;
			emptyLabel.Visible = true;
			return;
		}

		box.Setup(equipment);
		box.Visible = true;
		emptyLabel.Visible = false;
	}

	private void ConnectSlot(JyButton slotButton, CharacterEquipmentBox equipmentBox, EquipmentSlotType slotType)
	{
		slotButton.Pressed += () => ShowEquipmentSelection(slotType);
		slotButton.GuiInput += inputEvent => OnSlotGuiInput(inputEvent, slotType);
		equipmentBox.EquipmentPrimaryClicked += () => ShowEquipmentSelection(slotType);
		equipmentBox.EquipmentSecondaryClicked += () => Unequip(slotType);
	}

	private void OnSlotGuiInput(InputEvent inputEvent, EquipmentSlotType slotType)
	{
		if (inputEvent is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
		{
			return;
		}

		Unequip(slotType);
		GetViewport().SetInputAsHandled();
	}

	private void ShowEquipmentSelection(EquipmentSlotType slotType)
	{
		if (_character is null)
		{
			return;
		}

		if (EquipmentSelectionPanelScene is null)
		{
			throw new InvalidOperationException("EquipmentSelectionPanelScene is not assigned.");
		}

		var instance = EquipmentSelectionPanelScene.Instantiate();
		if (instance is not CharacterEquipmentSelectionPanel panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException("EquipmentSelectionPanel scene root must be CharacterEquipmentSelectionPanel.");
		}

		panel.Configure(_character.Id, slotType);
		UIRoot.Instance.ModalLayer.AddChild(panel);
	}

	private void Unequip(EquipmentSlotType slotType)
	{
		if (_character is null || _character.GetEquipment(slotType) is null)
		{
			UIRoot.Instance.ShowSuggestion("该装备槽为空。");
			return;
		}

		try
		{
			var equipment = Game.InventoryService.UnequipToInventory(_character, slotType);
			UIRoot.Instance.ShowToast($"卸下【{equipment.Definition.Name}】");
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Unequipping character equipment failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}
}
