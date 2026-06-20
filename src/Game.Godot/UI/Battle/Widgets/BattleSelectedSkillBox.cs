using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleSelectedSkillBox : JyButton
{
	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _formNameLabel = null!;

	private SkillInstance? _skill;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%SelectedSkillIcon");
		_nameLabel = GetNode<Label>("%SelectedSkillNameLabel");
		_formNameLabel = GetNode<Label>("%SelectedSkillFormNameLabel");
		Pressed += ShowSkillTooltip;
		Refresh();
	}

	public void Setup(SkillInstance? skill)
	{
		_skill = skill;
		Refresh();
	}

	private void ShowSkillTooltip()
	{
		if (_skill is null)
		{
			return;
		}

		if (CreateTooltip() is not { } tooltip)
		{
			return;
		}

		var popup = new PopupPanel();
		popup.AddChild(tooltip);
		AddChild(popup);
		PopupTooltip(popup, tooltip);
	}

	private Control? CreateTooltip()
	{
		if (_skill is null)
		{
			return null;
		}

		if (TooltipScene is null)
		{
			throw new InvalidOperationException("TooltipScene is not assigned.");
		}

		if (TooltipScene.Instantiate() is not SkillTooltip tooltip)
		{
			throw new InvalidOperationException("SkillTooltip scene root must be SkillTooltip.");
		}

		tooltip.Setup(_skill);
		return tooltip;
	}

	private void PopupTooltip(PopupPanel popup, Control tooltip)
	{
		var offset = new Vector2(Size.X + 12f, 0f);
		var viewportRect = GetViewportRect();
		var globalPosition = GlobalPosition + offset;
		var tooltipSize = tooltip.GetCombinedMinimumSize();

		if (globalPosition.X + tooltipSize.X > viewportRect.Size.X)
		{
			offset.X = -tooltipSize.X - 12f;
		}

		if (globalPosition.Y + tooltipSize.Y > viewportRect.Size.Y)
		{
			offset.Y = MathF.Max(-GlobalPosition.Y, viewportRect.Size.Y - GlobalPosition.Y - tooltipSize.Y);
		}

		popup.Popup(new Rect2I(
			(Vector2I)(GlobalPosition + offset),
			(Vector2I)tooltipSize));
	}

	private void Refresh()
	{
		if (!IsInsideTree())
		{
			return;
		}

		Disabled = _skill is null;
		MouseDefaultCursorShape = _skill is null
			? CursorShape.Arrow
			: CursorShape.PointingHand;

		if (_skill is null)
		{
			_avatar.Texture = null;
			_nameLabel.Text = "未选中技能";
			_formNameLabel.Text = string.Empty;
			return;
		}

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
			return;
		}

		_nameLabel.Text = skillName;
		_formNameLabel.Text = string.Empty;
	}
}
