using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleSkillBox : TextureButton
{
	private static readonly Color DisabledModulate = new(0.35f, 0.35f, 0.35f, 0.95f);

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _formNameLabel = null!;
	private Panel _selectedFrame = null!;

	private SkillInstance? _skill;
	private bool _available = true;
	private bool _selected;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_formNameLabel = GetNode<Label>("%FormNameLabel");
		_selectedFrame = GetNode<Panel>("%SelectedFrame");
		Refresh();
	}

	public void Setup(SkillInstance skill, bool selected, bool available)
	{
		ArgumentNullException.ThrowIfNull(skill);
		_skill = skill;
		_available = available;
		_selected = selected;
		TooltipText = skill.Name;
		Refresh();
	}

	public override Control? _MakeCustomTooltip(string forText)
	{
		if (_skill is null)
		{
			return null;
		}

		if (TooltipScene.Instantiate() is not SkillTooltip tooltip)
		{
			throw new InvalidOperationException("SkillTooltip scene root must be SkillTooltip.");
		}

		tooltip.Setup(_skill);
		return tooltip;
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _skill is null)
		{
			return;
		}

		_selectedFrame.Visible = _selected;
		Disabled = !_available;
		MouseDefaultCursorShape = _available
			? CursorShape.PointingHand
			: CursorShape.Arrow;
		Modulate = _available
			? Colors.White
			: DisabledModulate;
		_avatar.Texture = AssetResolver.LoadSkillIconResource(_skill.Icon);
		ApplySplitName(_skill.Name);
	}

	private void ApplySplitName(string skillName)
	{
		var segments = skillName.Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length == 2)
		{
			_nameLabel.Text = segments[0];
			_formNameLabel.Text = segments[1];
			_formNameLabel.Visible = true;
			return;
		}

		_nameLabel.Text = skillName;
		_formNameLabel.Text = string.Empty;
		_formNameLabel.Visible = false;
	}
}
