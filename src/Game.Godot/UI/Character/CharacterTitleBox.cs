using Game.Core.Model.Character;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

/// <summary>
/// One unlockable title inside the character panel title tab. Mirrors the
/// legacy SkillSelectItemUI title row and the SkillBox interaction split:
/// the badge checkbox (ActiveButton) equips or unequips the title, the
/// check mark shows the equipped state, pressing the box itself requests
/// the title detail panel, and hovering shows the title tooltip.
/// </summary>
public partial class CharacterTitleBox : Button
{
	public event Action<CharacterTitleInstance>? ToggleRequested;
	public event Action<CharacterTitleInstance>? DetailRequested;

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	private Label _nameLabel = null!;
	private TextureRect _avatar = null!;
	private TextureButton _activeButton = null!;
	private TextureRect _checkMark = null!;

	private CharacterTitleInstance? _title;
	private bool _isInteractive;

	public string TitleId => _title?.Id ?? string.Empty;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("%NameLabel");
		_avatar = GetNode<TextureRect>("%Avatar");
		_activeButton = GetNode<TextureButton>("%ActiveButton");
		_checkMark = GetNode<TextureRect>("%CheckMark");
		Pressed += OnPressed;
		_activeButton.Pressed += OnActiveButtonPressed;
		Refresh();
	}

	public void Setup(CharacterTitleInstance title, bool isInteractive)
	{
		ArgumentNullException.ThrowIfNull(title);
		_title = title;
		_isInteractive = isInteractive;
		TooltipText = title.Definition.Name;
		Refresh();
	}

	public override Control? _MakeCustomTooltip(string forText)
	{
		if (_title is null)
		{
			return null;
		}

		if (TooltipScene is null)
		{
			throw new InvalidOperationException("TooltipScene is not assigned.");
		}

		var instance = TooltipScene.Instantiate();
		if (instance is not TitleTooltip tooltip)
		{
			instance.QueueFree();
			throw new InvalidOperationException("TitleTooltip scene root must be TitleTooltip.");
		}

		tooltip.Setup(_title);
		return tooltip;
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _title is null)
		{
			return;
		}

		_activeButton.Disabled = !_isInteractive;
		_checkMark.Visible = _title.Equipped;
		_avatar.Texture = AssetResolver.LoadTexture(_title.Definition.Icon) ?? _avatar.Texture;
		_nameLabel.Text = _title.Definition.Name;
		ApplyLabelColor(_nameLabel, _title.Equipped ? Colors.Magenta : Colors.White);
	}

	private void OnPressed()
	{
		if (_title is not null)
		{
			DetailRequested?.Invoke(_title);
		}
	}

	private void OnActiveButtonPressed()
	{
		if (_title is null || !_isInteractive)
		{
			return;
		}

		ToggleRequested?.Invoke(_title);
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
