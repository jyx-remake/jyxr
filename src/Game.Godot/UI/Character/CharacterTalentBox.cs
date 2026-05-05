using Game.Core.Definitions;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterTalentBox : VBoxContainer
{
	private const string UnlockedFormatter = "[color=red]{0}[/color] [color=blue]【消耗武学常识:{1}】[/color]";
	private const string EffectiveFormatter = "[color=yellow]{0}[/color] [color=blue]【装备/技能】[/color]";

	private RichTextLabel _nameLabel = null!;
	private Label _descriptionLabel = null!;
	private TalentDefinition? _talent;
	private bool _isUnlocked;

	public override void _Ready()
	{
		_nameLabel = GetNode<RichTextLabel>("%NameLabel");
		_descriptionLabel = GetNode<Label>("%DescriptionLabel");
		Refresh();
	}

	public void Setup(TalentDefinition talent, bool isUnlocked)
	{
		ArgumentNullException.ThrowIfNull(talent);
		_talent = talent;
		_isUnlocked = isUnlocked;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _talent is null)
		{
			return;
		}

		_nameLabel.Text = string.Format(
			_isUnlocked ? UnlockedFormatter : EffectiveFormatter,
			_talent.Name,
			_talent.Point);
		_descriptionLabel.Text = _talent.Description;
	}
}
