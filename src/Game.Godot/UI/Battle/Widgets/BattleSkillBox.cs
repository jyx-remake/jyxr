using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleSkillBox : TextureButton
{
	public const float DesignWidth = 180f;
	public const float DesignHeight = 270f;

	private static readonly Color DisabledModulate = new(0.35f, 0.35f, 0.35f, 0.95f);
	private static readonly Rect2 IconPanelDesignRect = new(new Vector2(37f, 61f), new Vector2(140f, 139f));
	private static readonly Rect2 NameLabelDesignRect = new(new Vector2(-4f, -1f), new Vector2(39f, 269f));
	private static readonly Rect2 FormNameLabelDesignRect = new(new Vector2(38f, 14f), new Vector2(39f, 269f));
	private const int DesignFontSize = 40;
	private const int DesignOutlineSize = 3;
	private const int DesignIconMargin = 6;
	private const float MinPresentationScale = 0.42f;
	private const float MaxPresentationScale = 1f;

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	private Panel _iconPanel = null!;
	private MarginContainer _iconMargin = null!;
	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _formNameLabel = null!;
	private Panel _selectedFrame = null!;

	private SkillInstance? _skill;
	private bool _available = true;
	private bool _selected;
	private float _presentationScale = 1f;

	public override void _Ready()
	{
		_iconPanel = GetNode<Panel>("Panel");
		_iconMargin = GetNode<MarginContainer>("Panel/MarginContainer");
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_formNameLabel = GetNode<Label>("%FormNameLabel");
		_selectedFrame = GetNode<Panel>("%SelectedFrame");
		DuplicateLabelSettings(_nameLabel);
		DuplicateLabelSettings(_formNameLabel);
		ApplyPresentationScale();
		Refresh();
	}

	public void SetPresentationScale(float scale)
	{
		_presentationScale = Mathf.Clamp(scale, MinPresentationScale, MaxPresentationScale);
		ApplyPresentationScale();
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

	private void ApplyPresentationScale()
	{
		var size = new Vector2(DesignWidth, DesignHeight) * _presentationScale;
		CustomMinimumSize = size;
		Size = size;

		if (!IsInsideTree())
		{
			return;
		}

		_iconPanel.Position = IconPanelDesignRect.Position * _presentationScale;
		_iconPanel.Size = IconPanelDesignRect.Size * _presentationScale;
		_nameLabel.Position = NameLabelDesignRect.Position * _presentationScale;
		_nameLabel.Size = NameLabelDesignRect.Size * _presentationScale;
		_formNameLabel.Position = FormNameLabelDesignRect.Position * _presentationScale;
		_formNameLabel.Size = FormNameLabelDesignRect.Size * _presentationScale;

		var margin = Math.Max(2, (int)MathF.Round(DesignIconMargin * _presentationScale));
		_iconMargin.AddThemeConstantOverride("margin_left", margin);
		_iconMargin.AddThemeConstantOverride("margin_top", margin);
		_iconMargin.AddThemeConstantOverride("margin_right", margin);
		_iconMargin.AddThemeConstantOverride("margin_bottom", margin);

		var fontSize = Math.Max(18, (int)MathF.Round(DesignFontSize * _presentationScale));
		var outlineSize = Math.Max(1, (int)MathF.Round(DesignOutlineSize * _presentationScale));
		ApplyLabelScale(_nameLabel, fontSize, outlineSize);
		ApplyLabelScale(_formNameLabel, fontSize, outlineSize);
	}

	private static void DuplicateLabelSettings(Label label)
	{
		if (label.LabelSettings is not null)
		{
			label.LabelSettings = (LabelSettings)label.LabelSettings.Duplicate();
		}
	}

	private static void ApplyLabelScale(Label label, int fontSize, int outlineSize)
	{
		if (label.LabelSettings is null)
		{
			label.AddThemeFontSizeOverride("font_size", fontSize);
			label.AddThemeConstantOverride("outline_size", outlineSize);
			return;
		}

		label.LabelSettings.FontSize = fontSize;
		label.LabelSettings.OutlineSize = outlineSize;
	}
}
