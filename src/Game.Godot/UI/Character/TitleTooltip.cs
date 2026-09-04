using Game.Application.Formatters;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

public partial class TitleTooltip : PanelContainer
{
	private Label _nameLabel = null!;
	private RichTextLabel _contentLabel = null!;

	private CharacterTitleInstance? _title;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("%NameLabel");
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		Refresh();
	}

	public void Setup(CharacterTitleInstance title)
	{
		ArgumentNullException.ThrowIfNull(title);
		_title = title;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _title is null)
		{
			return;
		}

		_nameLabel.Text = _title.Definition.Name;
		ApplyLabelColor(_nameLabel, Colors.Magenta);
		_contentLabel.Text = TitleDescriptionFormatter.FormatBbCodeCn(_title.Definition, Game.ContentRepository);
	}

	private static void ApplyLabelColor(Label label, Color color)
	{
		label.AddThemeColorOverride("font_color", color);

		if (label.LabelSettings is null)
		{
			return;
		}

		var labelSettings = (LabelSettings)label.LabelSettings.Duplicate();
		labelSettings.FontColor = color;
		label.LabelSettings = labelSettings;
	}
}
