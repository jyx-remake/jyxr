using Game.Core.Model.Skills;
using Game.Core.Battle;
using Game.Godot.Assets;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleSkillBox : Button
{
	private static readonly Color DisabledModulate = new(0.35f, 0.35f, 0.35f, 0.95f);

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	private TextureRect _avatar = null!;
	private TextureRect _sealOverlay = null!;
	private Label _nameLabel = null!;
	private Label _formNameLabel = null!;

	private SkillInstance? _skill;
	private BattleSkillAvailability? _availability;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_sealOverlay = GetNode<TextureRect>("%SealOverlay");
		_nameLabel = GetNode<Label>("%NameLabel");
		_formNameLabel = GetNode<Label>("%FormNameLabel");
		Refresh();
	}

	public void Setup(SkillInstance skill, bool selected, BattleSkillAvailability availability)
	{
		ArgumentNullException.ThrowIfNull(skill);
		ArgumentNullException.ThrowIfNull(availability);
		_skill = skill;
		_availability = availability;
		SetPressedNoSignal(selected);
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

		var available = _availability?.IsAvailable == true;
		Disabled = !available;
		MouseDefaultCursorShape = available
			? CursorShape.PointingHand
			: CursorShape.Arrow;
		Modulate = available
			? Colors.White
			: DisabledModulate;
		_avatar.Texture = AssetResolver.LoadSkillIconResource(_skill.Icon);
		_sealOverlay.Visible = _availability?.IsSealed == true;
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
