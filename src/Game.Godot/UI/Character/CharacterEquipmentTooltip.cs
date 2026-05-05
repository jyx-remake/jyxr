using Game.Application.Formatters;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterEquipmentTooltip : PanelContainer
{
	private Label _nameLabel = null!;
	private RichTextLabel _contentLabel = null!;

	private EquipmentInstance? _equipment;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("%NameLabel");
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		Refresh();
	}

	public void Setup(EquipmentInstance equipment)
	{
		ArgumentNullException.ThrowIfNull(equipment);
		_equipment = equipment;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _equipment is null)
		{
			return;
		}

		_nameLabel.Text = _equipment.Definition.Name;
		_contentLabel.Text = ItemDescriptionFormatter.FormatBbCodeCn(_equipment, Game.ContentRepository);
	}
}
