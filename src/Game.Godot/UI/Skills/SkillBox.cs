using System.Globalization;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class SkillBox : Control
{
	public event Action<SkillInstance>? ToggleRequested;

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	private Label _nameLabel = null!;
	private Label _formNameLabel = null!;
	private Label _levelLabel = null!;
	private TextureRect _avatar = null!;
	private TextureButton _activeButton = null!;
	private TextureRect _checkMark = null!;

	private SkillInstance? _skill;
	private bool _isInteractive;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("%NameLabel");
		_formNameLabel = GetNode<Label>("%FormNameLabel");
		_levelLabel = GetNode<Label>("%LevelLabel");
		_avatar = GetNode<TextureRect>("%Avatar");
		_activeButton = GetNode<TextureButton>("%ActiveButton");
		_checkMark = GetNode<TextureRect>("%CheckMark");
		_activeButton.Pressed += OnActiveButtonPressed;
		Refresh();
	}

	public void Setup(SkillInstance skill, bool isInteractive)
	{
		ArgumentNullException.ThrowIfNull(skill);
		_skill = skill;
		_isInteractive = isInteractive;
		TooltipText = skill.Name;
		Refresh();
	}

	public override Control? _MakeCustomTooltip(string forText)
	{
		if (_skill is null)
		{
			return null;
		}

		if (TooltipScene is null)
		{
			throw new InvalidOperationException("TooltipScene is not assigned.");
		}

		var instance = TooltipScene.Instantiate();
		if (instance is not SkillTooltip tooltip)
		{
			instance.QueueFree();
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

		if (_skill is FormSkillInstance formSkill)
		{
			_activeButton.Visible = false;
			_checkMark.Visible = false;
			_avatar.Texture = ResolveTexture(formSkill) ?? _avatar.Texture;
			_levelLabel.Visible = false;
			_nameLabel.Visible = false;
			_formNameLabel.Text = formSkill.Name;
			_formNameLabel.Visible = true;
			ApplyLabelColor(_formNameLabel, Colors.Red);
			return;
		}

		_activeButton.Visible = true;
		_activeButton.Disabled = !_isInteractive || !CanToggle(_skill);
		_checkMark.Visible = _skill.IsActive;
		_avatar.Texture = ResolveTexture(_skill) ?? _avatar.Texture;
		_nameLabel.Visible = true;
		_nameLabel.Text = _skill.Name;
		ApplyLabelColor(_nameLabel, ResolveNameColor(_skill));
		_formNameLabel.Visible = false;
		_levelLabel.Visible = ShouldShowLevel(_skill);
		_levelLabel.Text = _skill.Level.ToString(CultureInfo.InvariantCulture);
	}

	private void OnActiveButtonPressed()
	{
		if (_skill is null || !_isInteractive || !CanToggle(_skill))
		{
			return;
		}

		ToggleRequested?.Invoke(_skill);
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

	private static bool ShouldShowLevel(SkillInstance skill) =>
		skill is ExternalSkillInstance or InternalSkillInstance;

	private static bool CanToggle(SkillInstance skill) =>
		skill is ExternalSkillInstance or InternalSkillInstance or SpecialSkillInstance;

	private static Color ResolveNameColor(SkillInstance skill) =>
		skill switch
		{
			InternalSkillInstance => Colors.Magenta,
			SpecialSkillInstance => Colors.CornflowerBlue,
			_ => Colors.White,
		};

	private static Texture2D? ResolveTexture(SkillInstance skill)
	{
		var texture = AssetResolver.LoadSkillIconResource(skill.Icon);
		if (texture is not null)
		{
			return texture;
		}

		if (skill is FormSkillInstance formSkill)
		{
			return AssetResolver.LoadSkillIconResource(formSkill.Parent.Icon);
		}

		return null;
	}
}
