using Game.Core.Model.Character;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterRosterAvatarButton : Button
{
	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Control _selectedFrame = null!;
	private CharacterInstance? _character;
	private bool _isSelected;

	[Signal]
	public delegate void CharacterSelectedEventHandler(string characterId);

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_selectedFrame = GetNode<Control>("%SelectedFrame");
		Pressed += OnPressed;
		RefreshView();
	}

	public void Setup(CharacterInstance character, bool isSelected)
	{
		ArgumentNullException.ThrowIfNull(character);
		_character = character;
		_isSelected = isSelected;
		RefreshView();
	}

	private void RefreshView()
	{
		if (_character is null || !IsInsideTree())
		{
			return;
		}

		TooltipText = _character.Name;
		_nameLabel.Text = _character.Name;
		_selectedFrame.Visible = _isSelected;

		var portrait = AssetResolver.LoadCharacterPortrait(_character);
		if (portrait is not null)
		{
			_avatar.Texture = portrait;
		}
	}

	private void OnPressed()
	{
		if (_character is null)
		{
			return;
		}

		global::Game.Godot.Game.Audio.PlaySfx("音效.UI.点击");
		EmitSignal(SignalName.CharacterSelected, _character.Id);
	}
}
