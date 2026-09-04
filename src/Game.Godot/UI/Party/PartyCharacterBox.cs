using Game.Application.Formatters;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class PartyCharacterBox : Button
{
	private const double MobileLongPressSeconds = 0.35d;
	private const float MobileLongPressMoveTolerance = 16f;
	private const int MobileDragStartVibrationMilliseconds = 35;
	private const float MobileDragStartVibrationAmplitude = 0.45f;

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _levelLabel = null!;
	private Label _attackLabel = null!;
	private Label _defenceLabel = null!;
	private TextureRect _maleLogo = null!;
	private TextureRect _femaleLogo = null!;
	private Label _lockLabel = null!;

	private CharacterInstance? _character;
	private PartyPanel? _ownerPanel;
	private int _partyIndex;
	private bool _mobileLongPressPending;
	private bool _suppressSelection;
	private int _mobileTouchIndex = -1;
	private double _mobileLongPressElapsed;
	private Vector2 _mobileTouchOrigin;
	private Vector2 _mobileTouchPosition;

	[Signal]
	public delegate void CharacterSelectedEventHandler(string characterId);

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

		Pressed += OnPressed;
		SetProcess(false);
		RefreshView();
	}

	public override void _ExitTree()
	{
		CancelMobileLongPress();
		_ownerPanel?.EndCharacterDrag();
	}

	public override void _Process(double delta)
	{
		if (!_mobileLongPressPending)
		{
			SetProcess(false);
			return;
		}

		_mobileLongPressElapsed += delta;
		if (_mobileLongPressElapsed < MobileLongPressSeconds)
		{
			return;
		}

		_mobileLongPressPending = false;
		SetProcess(false);
		StartMobileDrag();
	}

	public override void _GuiInput(InputEvent @event)
	{
		base._GuiInput(@event);
		if (!Game.IsMobilePlatform || _character is null || IsHeroLocked)
		{
			return;
		}

		switch (@event)
		{
			case InputEventScreenTouch touch when touch.Pressed:
				BeginMobileLongPress(touch);
				break;
			case InputEventScreenTouch touch when touch.Index == _mobileTouchIndex:
				_mobileTouchPosition = touch.Position;
				CancelMobileLongPress();
				if (_suppressSelection)
				{
					CallDeferred(MethodName.ResetSelectionSuppression);
				}

				break;
			case InputEventScreenDrag drag when drag.Index == _mobileTouchIndex:
				_mobileTouchPosition = drag.Position;
				if (_mobileLongPressPending &&
					_mobileTouchOrigin.DistanceSquaredTo(drag.Position) >
					MobileLongPressMoveTolerance * MobileLongPressMoveTolerance)
				{
					CancelMobileLongPress();
				}

				break;
		}
	}

	/// <summary>
	/// The owner panel drives drag-to-reorder. Pass null for read-only card
	/// lists (such as the recall panel) where dragging is not supported.
	/// </summary>
	public void Setup(CharacterInstance character, int partyIndex, PartyPanel? ownerPanel)
	{
		ArgumentNullException.ThrowIfNull(character);
		_character = character;
		_partyIndex = partyIndex;
		_ownerPanel = ownerPanel;
		RefreshView();
	}

	public void RefreshView()
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

		var portrait = AssetResolver.LoadTexture(_character.Portrait);
		if (portrait is not null)
		{
			_avatar.Texture = portrait;
		}

		_maleLogo.Visible = _character.Gender == CharacterGender.Male;
		_femaleLogo.Visible = _character.Gender == CharacterGender.Female;
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (Game.IsMobilePlatform || _character is null || IsHeroLocked || _ownerPanel is null)
		{
			return default;
		}

		_ownerPanel.BeginCharacterDrag(
			_character.Id,
			_partyIndex,
			GetGlobalRect().Position + atPosition);
		SetDragPreview(CreateDragPreview());
		return Variant.CreateFrom(_character.Id);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationScrollBegin)
		{
			CancelMobileLongPress();
		}
		else if (what == NotificationDragEnd)
		{
			_ownerPanel?.EndCharacterDrag();
			if (_suppressSelection && IsInsideTree())
			{
				CallDeferred(MethodName.ResetSelectionSuppression);
			}
		}
	}

	private void OnPressed()
	{
		if (_character is null || _suppressSelection)
		{
			return;
		}

		EmitSignal(SignalName.CharacterSelected, _character.Id);
	}

	private bool IsHeroLocked =>
		_character is not null &&
		_partyIndex == 0 &&
		string.Equals(_character.Id, Party.HeroCharacterId, StringComparison.Ordinal);

	private void BeginMobileLongPress(InputEventScreenTouch touch)
	{
		_mobileTouchIndex = touch.Index;
		_mobileTouchOrigin = touch.Position;
		_mobileTouchPosition = touch.Position;
		_mobileLongPressElapsed = 0d;
		_mobileLongPressPending = true;
		SetProcess(true);
	}

	private void CancelMobileLongPress()
	{
		_mobileLongPressPending = false;
		_mobileLongPressElapsed = 0d;
		SetProcess(false);
	}

	private void StartMobileDrag()
	{
		if (_character is null || _ownerPanel is null || IsHeroLocked)
		{
			return;
		}

		_suppressSelection = true;
		_ownerPanel.BeginCharacterDrag(
			_character.Id,
			_partyIndex,
			_mobileTouchPosition,
			_mobileTouchIndex);
		Input.VibrateHandheld(
			MobileDragStartVibrationMilliseconds,
			MobileDragStartVibrationAmplitude);
		ForceDrag(Variant.CreateFrom(_character.Id), CreateDragPreview());
	}

	private void ResetSelectionSuppression()
	{
		_suppressSelection = false;
		_mobileTouchIndex = -1;
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
}
