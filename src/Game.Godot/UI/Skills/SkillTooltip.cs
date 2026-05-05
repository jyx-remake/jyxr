using Game.Application.Formatters;
using Game.Core.Model.Skills;
using Godot;

namespace Game.Godot.UI;

public partial class SkillTooltip : PanelContainer
{
	private Label _nameLabel = null!;
	private RichTextLabel _contentLabel = null!;

	private SkillInstance? _skill;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("%NameLabel");
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		Refresh();
	}

	public void Setup(SkillInstance skill)
	{
		ArgumentNullException.ThrowIfNull(skill);
		_skill = skill;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _skill is null)
		{
			return;
		}

		_nameLabel.Text = _skill.Name;
		ApplyLabelColor(_nameLabel, ResolveTitleColor(_skill));
		_contentLabel.Text = SkillDescriptionFormatter.FormatBbCodeCn(_skill, Game.ContentRepository);
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

	private static Color ResolveTitleColor(SkillInstance skill) =>
		skill switch
		{
			InternalSkillInstance => Colors.Magenta,
			SpecialSkillInstance => Colors.CornflowerBlue,
			FormSkillInstance => Colors.Red,
			_ => Colors.White,
		};
}
