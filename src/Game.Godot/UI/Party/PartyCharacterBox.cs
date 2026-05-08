using Game.Application.Formatters;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class PartyCharacterBox : Button
{
	private const string DragTargetGroupName = "party_character_boxes";

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _levelLabel = null!;
	private Label _attackLabel = null!;
	private Label _defenceLabel = null!;
	private TextureRect _maleLogo = null!;
	private TextureRect _femaleLogo = null!;
	private Label _lockLabel = null!;
	private ColorRect _dragHighlight = null!;

	private CharacterInstance? _character;
	private int _partyIndex;

	[Signal]
	public delegate void CharacterSelectedEventHandler(string characterId);

	[Signal]
	public delegate void CharacterMoveRequestedEventHandler(string characterId, int targetIndex);

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_levelLabel = GetNode<Label>("%LevelLabel");
		_attackLabel = GetNode<Label>("%AttackLabel");
		_defenceLabel = GetNode<Label>("%DefenceLabel");
		_maleLogo = GetNode<TextureRect>("%MaleLogo");
		_femaleLogo = GetNode<TextureRect>("%FemaleLogo");
		_lockLabel = GetNode<Label>("%LockLabel");
		_dragHighlight = GetNode<ColorRect>("%DragHighlight");

		AddToGroup(DragTargetGroupName);
		Pressed += OnPressed;
		ClearDropHighlight();
		Refresh();
	}

	public void Setup(CharacterInstance character, int partyIndex)
	{
		ArgumentNullException.ThrowIfNull(character);
		_character = character;
		_partyIndex = partyIndex;
		Refresh();
	}

	private void Refresh()
	{
		if (_character is null || !IsInsideTree())
		{
			return;
		}

		_nameLabel.Text = _character.Name;
		_levelLabel.Text = $"等级:{_character.Level}";
		var combatStats = CharacterCombatStatFormatter.Calculate(_character);
		_attackLabel.Text = $"攻:{combatStats.Attack}";
		_defenceLabel.Text = $"防:{combatStats.Defence}";
		_lockLabel.Visible = IsHeroLocked;
		TooltipText = string.Empty;

		var portrait = AssetResolver.LoadCharacterPortrait(_character);
		if (portrait is not null)
		{
			_avatar.Texture = portrait;
		}

		_maleLogo.Visible = _character.Definition.Gender == CharacterGender.Male;
		_femaleLogo.Visible = _character.Definition.Gender == CharacterGender.Female;
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (_character is null || IsHeroLocked)
		{
			return default;
		}

		SetDragPreview(CreateDragPreview());
		return Variant.CreateFrom(_character.Id);
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		var canDrop = CanAcceptDraggedCharacter(data);
		SetDropHighlight(canDrop);
		return canDrop;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (_character is null || !TryGetDraggedCharacterId(data, out var characterId))
		{
			ClearDropHighlight();
			return;
		}

		EmitSignal(SignalName.CharacterMoveRequested, characterId, _partyIndex);
		ClearDropHighlight();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationDragEnd && IsInsideTree())
		{
			GetTree().CallGroup(DragTargetGroupName, nameof(ClearDropHighlight));
		}
	}

	public void ClearDropHighlight()
	{
		SetDropHighlight(false);
	}

	private void OnPressed()
	{
		if (_character is null)
		{
			return;
		}

		EmitSignal(SignalName.CharacterSelected, _character.Id);
	}

	private bool IsHeroLocked =>
		_character is not null &&
		_partyIndex == 0 &&
		string.Equals(_character.Id, Party.HeroCharacterId, StringComparison.Ordinal);

	private bool CanAcceptDraggedCharacter(Variant data)
	{
		if (_character is null || IsHeroLocked)
		{
			return false;
		}

		if (!TryGetDraggedCharacterId(data, out var characterId))
		{
			return false;
		}

		return !string.Equals(characterId, _character.Id, StringComparison.Ordinal) &&
			!string.Equals(characterId, Party.HeroCharacterId, StringComparison.Ordinal);
	}

	private static bool TryGetDraggedCharacterId(Variant data, out string characterId)
	{
		characterId = data.AsString();
		return !string.IsNullOrWhiteSpace(characterId);
	}

	private Control CreateDragPreview()
	{
		var preview = new Control
		{
			CustomMinimumSize = Size,
			Size = Size,
			MouseFilter = MouseFilterEnum.Ignore,
		};

		if (GetNode<Control>("%Content").Duplicate() is Control contentCopy)
		{
			contentCopy.MouseFilter = MouseFilterEnum.Ignore;
			contentCopy.Modulate = new Color(1f, 1f, 1f, 0.88f);
			preview.AddChild(contentCopy);
		}

		return preview;
	}

	private void SetDropHighlight(bool highlighted)
	{
		if (_dragHighlight is not null)
		{
			_dragHighlight.Visible = highlighted;
		}
	}
}
