using Game.Application.Formatters;
using Game.Core.Definitions;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class InventoryItemTooltip : PanelContainer
{
	private Label _nameLabel = null!;
	private RichTextLabel _contentLabel = null!;
	private InventoryEntry? _entry;
	private EquipmentInstance? _equipment;
	private ItemDefinition? _item;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("%NameLabel");
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		Refresh();
	}

	public void Setup(InventoryEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry);
		_entry = entry;
		_equipment = null;
		_item = null;
		Refresh();
	}

	public void Setup(EquipmentInstance equipment)
	{
		ArgumentNullException.ThrowIfNull(equipment);
		_equipment = equipment;
		_entry = null;
		_item = null;
		Refresh();
	}

	public void Setup(ItemDefinition item)
	{
		ArgumentNullException.ThrowIfNull(item);
		_item = item;
		_entry = null;
		_equipment = null;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree())
		{
			return;
		}

		if (_equipment is not null)
		{
			_nameLabel.Text = _equipment.Definition.Name;
			_contentLabel.Text = ItemDescriptionFormatter.FormatBbCodeCn(_equipment, Game.ContentRepository);
			return;
		}

		if (_item is not null)
		{
			_nameLabel.Text = _item.Name;
			_contentLabel.Text = ItemDescriptionFormatter.FormatBbCodeCn(_item, Game.ContentRepository);
			return;
		}

		if (_entry is null)
		{
			return;
		}

		_nameLabel.Text = _entry.Definition.Name;
		_contentLabel.Text = _entry switch
		{
			EquipmentInstanceInventoryEntry equipment => ItemDescriptionFormatter.FormatBbCodeCn(equipment.Equipment, Game.ContentRepository),
			_ => ItemDescriptionFormatter.FormatBbCodeCn(_entry.Definition, Game.ContentRepository),
		};
	}
}
