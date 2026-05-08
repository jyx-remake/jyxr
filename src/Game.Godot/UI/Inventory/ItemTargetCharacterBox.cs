using Game.Application;
using Game.Application.Formatters;
using Game.Core.Model.Character;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class ItemTargetCharacterBox : Button
{
	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _levelLabel = null!;
	private Label _attackLabel = null!;
	private Label _defenceLabel = null!;
	private ColorRect _disabledOverlay = null!;

	private CharacterInstance? _character;
	private ItemUseTargetCandidate? _candidate;

	[Signal]
	public delegate void TargetSelectedEventHandler(string characterId);

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_levelLabel = GetNode<Label>("%LevelLabel");
		_attackLabel = GetNode<Label>("%AttackLabel");
		_defenceLabel = GetNode<Label>("%DefenceLabel");
		_disabledOverlay = GetNode<ColorRect>("%DisabledOverlay");
		Pressed += OnPressed;
		Refresh();
	}

	public void Setup(CharacterInstance character, ItemUseTargetCandidate candidate)
	{
		ArgumentNullException.ThrowIfNull(character);
		ArgumentNullException.ThrowIfNull(candidate);
		_character = character;
		_candidate = candidate;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _character is null || _candidate is null)
		{
			return;
		}

		_nameLabel.Text = _character.Name;
		_levelLabel.Text = $"等级:{_character.Level}";
		var combatStats = CharacterCombatStatFormatter.Calculate(_character);
		_attackLabel.Text = $"攻:{combatStats.Attack}";
		_defenceLabel.Text = $"防:{combatStats.Defence}";

		var portrait = AssetResolver.LoadCharacterPortrait(_character);
		if (portrait is not null)
		{
			_avatar.Texture = portrait;
		}

		var enabled = _candidate.CanUse;
		_avatar.Modulate = enabled ? Colors.White : Colors.Black;
		_disabledOverlay.Visible = !enabled;
		TooltipText = enabled ? _character.Name : _candidate.Reason;
	}

	private void OnPressed()
	{
		if (_character is null || _candidate is null)
		{
			return;
		}

		if (!_candidate.CanUse)
		{
			UIRoot.Instance.ShowSuggestion(_candidate.Reason);
			return;
		}

		EmitSignal(SignalName.TargetSelected, _character.Id);
	}
}
