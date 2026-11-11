using Game.Application;
using Game.Core.Model;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

public partial class PartyPanel : JyPanel
{
	private const float AutoScrollEdgeSize = 96f;
	private const float AutoScrollMaximumSpeed = 1600f;
	private const float MobileAutoScrollActivationDistance = 8f;
	private const float InsertionIndicatorThickness = 8f;
	private const float InsertionIndicatorVerticalInset = 12f;
	private const int MobileDropVibrationMilliseconds = 24;
	private const float MobileDropVibrationAmplitude = 0.35f;

	[Export]
	public PackedScene PartyCharacterBoxScene { get; set; } = null!;

	private ScrollContainer _scrollContainer = null!;
	private VirtualGrid _gridContainer = null!;
	private PartyDropSurface _dragOverlay = null!;
	private ColorRect _insertionIndicator = null!;
	private Label _hintLabel = null!;
	private Label _emptyLabel = null!;
	private readonly List<IDisposable> _subscriptions = [];
	private IReadOnlyList<CharacterInstance> _members = [];

	private string? _draggedCharacterId;
	private int _draggedCharacterIndex = -1;
	private int _dragTouchIndex = -1;
	private int? _dropTargetIndex;
	private Vector2 _dragPointerPosition;
	private Vector2 _dragStartPointerPosition;
	private bool _autoScrollArmed;
	private Control.MouseFilterEnum _scrollMouseFilterBeforeDrag;

	public override void _Ready()
	{
		base._Ready();
		_scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
		_gridContainer = GetNode<VirtualGrid>("%VirtualGrid");
		_dragOverlay = GetNode<PartyDropSurface>("%DragOverlay");
		_insertionIndicator = GetNode<ColorRect>("%InsertionIndicator");
		_hintLabel = GetNode<Label>("%HintLabel");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_dragOverlay.Setup(this);
		_dragOverlay.Visible = false;
		_insertionIndicator.Visible = false;
		_hintLabel.Text = Game.IsMobilePlatform
			? "长按并拖拽队友卡片可调整顺序，主角固定在队伍首位。"
			: "拖拽队友卡片可调整顺序，主角固定在队伍首位。";
		SetProcess(false);
		_subscriptions.Add(Game.Session.Events.Subscribe<PartyChangedEvent>(OnPartyChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<CharacterChangedEvent>(OnCharacterChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<SaveLoadedEvent>(OnSaveLoaded));
		Refresh();
	}

	public override void _ExitTree()
	{
		EndCharacterDrag();
		foreach (var subscription in _subscriptions)
		{
			subscription.Dispose();
		}

		_subscriptions.Clear();
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsCharacterDragActive)
		{
			return;
		}

		switch (@event)
		{
			case InputEventMouseMotion mouseMotion when Game.IsDesktopPlatform:
				_dragPointerPosition = mouseMotion.Position;
				break;
			case InputEventScreenDrag drag when drag.Index == _dragTouchIndex:
				_dragPointerPosition = drag.Position;
				if (!_autoScrollArmed &&
					_dragStartPointerPosition.DistanceSquaredTo(drag.Position) >=
					MobileAutoScrollActivationDistance * MobileAutoScrollActivationDistance)
				{
					_autoScrollArmed = true;
				}

				break;
			case InputEventScreenTouch touch
				when touch.Index == _dragTouchIndex:
				_dragPointerPosition = touch.Position;
				break;
		}
	}

	public override void _Process(double delta)
	{
		if (!IsCharacterDragActive)
		{
			SetProcess(false);
			return;
		}

		if (Game.IsDesktopPlatform)
		{
			_dragPointerPosition = GetViewport().GetMousePosition();
		}

		AutoScroll(delta);
		UpdateInsertionTarget();
	}

	internal bool CanDropCharacter(Vector2 pointerPosition, Variant data)
	{
		if (!MatchesActiveDrag(data) ||
			!IsPointerInsideScroll(pointerPosition))
		{
			return false;
		}

		_dragPointerPosition = pointerPosition;
		UpdateInsertionTarget();
		return _dropTargetIndex.HasValue;
	}

	internal void DropCharacter(Vector2 pointerPosition, Variant data)
	{
		if (!CanDropCharacter(pointerPosition, data))
		{
			return;
		}

		CommitDrop();
	}

	internal void HandleDropSurfaceInput(InputEvent @event)
	{
		if (!Game.IsDesktopPlatform ||
			@event is not InputEventMouseButton { Pressed: true } mouseButton)
		{
			return;
		}

		const int wheelScrollStep = 80;
		switch (mouseButton.ButtonIndex)
		{
			case MouseButton.WheelUp:
				_scrollContainer.ScrollVertical -= wheelScrollStep;
				break;
			case MouseButton.WheelDown:
				_scrollContainer.ScrollVertical += wheelScrollStep;
				break;
		}
	}

	internal void BeginCharacterDrag(
		string characterId,
		int characterIndex,
		Vector2 pointerPosition,
		int touchIndex = -1)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
		if (characterIndex <= 0 ||
			characterIndex >= _members.Count ||
			string.Equals(characterId, Party.HeroCharacterId, StringComparison.Ordinal))
		{
			return;
		}

		EndCharacterDrag();
		_draggedCharacterId = characterId;
		_draggedCharacterIndex = characterIndex;
		_gridContainer.PinItem(characterIndex);
		_dragTouchIndex = touchIndex;
		_dragPointerPosition = pointerPosition;
		_dragStartPointerPosition = pointerPosition;
		_autoScrollArmed = Game.IsDesktopPlatform;
		_scrollMouseFilterBeforeDrag = _scrollContainer.MouseFilter;
		if (Game.IsMobilePlatform)
		{
			_scrollContainer.MouseFilter = MouseFilterEnum.Ignore;
		}

		_dragOverlay.Visible = true;
		SetProcess(true);
		UpdateInsertionTarget();
	}

	internal void EndCharacterDrag()
	{
		if (Game.IsMobilePlatform && IsCharacterDragActive && _scrollContainer is not null)
		{
			_scrollContainer.MouseFilter = _scrollMouseFilterBeforeDrag;
		}

		_gridContainer?.PinItem(null);
		_draggedCharacterId = null;
		_draggedCharacterIndex = -1;
		_dragTouchIndex = -1;
		_dropTargetIndex = null;
		_autoScrollArmed = false;
		if (_dragOverlay is not null)
		{
			_dragOverlay.Visible = false;
		}

		if (_insertionIndicator is not null)
		{
			_insertionIndicator.Visible = false;
		}

		SetProcess(false);
	}

	private void Refresh()
	{
		EndCharacterDrag();
		_members = Game.State.Party.Members;
		_emptyLabel.Visible = _members.Count == 0;
		_gridContainer.SetItems(_members, CreateCharacterBox,
			(box, character, index) => box.Setup(character, index, this));
	}

	private PartyCharacterBox CreateCharacterBox()
	{
		if (PartyCharacterBoxScene is null)
		{
			throw new InvalidOperationException("PartyCharacterBoxScene is not assigned.");
		}

		var instance = PartyCharacterBoxScene.Instantiate();
		if (instance is not PartyCharacterBox characterBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("PartyCharacterBox scene root must be PartyCharacterBox.");
		}

		characterBox.CharacterSelected += OnCharacterSelected;
		return characterBox;
	}

	private void OnCharacterSelected(string characterId)
	{
		UIRoot.Instance.ShowCharacterRosterPanel(characterId);
	}

	private void CommitDrop()
	{
		if (!IsCharacterDragActive ||
			_draggedCharacterId is null ||
			!IsPointerInsideScroll(_dragPointerPosition))
		{
			EndCharacterDrag();
			return;
		}

		UpdateInsertionTarget();
		if (_dropTargetIndex is not int targetIndex)
		{
			EndCharacterDrag();
			return;
		}

		var characterId = _draggedCharacterId;
		EndCharacterDrag();
		Game.PartyService.MoveMember(characterId, targetIndex);
		if (Game.IsMobilePlatform)
		{
			Input.VibrateHandheld(
				MobileDropVibrationMilliseconds,
				MobileDropVibrationAmplitude);
		}
	}

	private void OnPartyChanged(PartyChangedEvent _) => Refresh();

	private void OnCharacterChanged(CharacterChangedEvent sessionEvent)
	{
		for (var index = 0; index < _members.Count; index++)
		{
			if (_members[index].Id == sessionEvent.CharacterId)
			{
				_gridContainer.RefreshItem(index);
				return;
			}
		}
	}

	private void OnSaveLoaded(SaveLoadedEvent _) => Refresh();

	private bool IsPointerInsideScroll(Vector2 pointerPosition) =>
		new Rect2(Vector2.Zero, _scrollContainer.Size).HasPoint(
			_scrollContainer.GetGlobalTransformWithCanvas().AffineInverse() * pointerPosition);

	private bool IsCharacterDragActive => _draggedCharacterId is not null;

	private bool MatchesActiveDrag(Variant data) =>
		IsCharacterDragActive &&
		string.Equals(
			data.AsString(),
			_draggedCharacterId,
			StringComparison.Ordinal);

	private void AutoScroll(double delta)
	{
		if (!_autoScrollArmed)
		{
			return;
		}

		var pointerPosition = _scrollContainer.GetGlobalTransformWithCanvas().AffineInverse() * _dragPointerPosition;
		var scrollRect = new Rect2(Vector2.Zero, _scrollContainer.Size);
		var speed = 0f;
		if (pointerPosition.Y < scrollRect.Position.Y + AutoScrollEdgeSize)
		{
			var intensity = Mathf.Clamp(
				(scrollRect.Position.Y + AutoScrollEdgeSize - pointerPosition.Y) /
				AutoScrollEdgeSize,
				0f,
				1f);
			speed = -AutoScrollMaximumSpeed * intensity;
		}
		else if (pointerPosition.Y > scrollRect.End.Y - AutoScrollEdgeSize)
		{
			var intensity = Mathf.Clamp(
				(pointerPosition.Y - (scrollRect.End.Y - AutoScrollEdgeSize)) /
				AutoScrollEdgeSize,
				0f,
				1f);
			speed = AutoScrollMaximumSpeed * intensity;
		}

		if (Mathf.IsZeroApprox(speed))
		{
			return;
		}

		var scrollBar = _scrollContainer.GetVScrollBar();
		var maximumScroll = Mathf.Max(0d, scrollBar.MaxValue - scrollBar.Page);
		var nextScroll = Mathf.Clamp(
			_scrollContainer.ScrollVertical + speed * (float)delta,
			0d,
			maximumScroll);
		_scrollContainer.ScrollVertical = Mathf.RoundToInt(nextScroll);
	}

	private void UpdateInsertionTarget()
	{
		_dropTargetIndex = null;
		_insertionIndicator.Visible = false;
		if (!IsCharacterDragActive || _members.Count < 2)
		{
			return;
		}

		var insertionSlot = ResolveInsertionSlot(_dragPointerPosition);
		if (insertionSlot <= 0)
		{
			return;
		}

		var targetIndex = insertionSlot > _draggedCharacterIndex
			? insertionSlot - 1
			: insertionSlot;
		if (targetIndex <= 0 ||
			targetIndex >= _members.Count ||
			targetIndex == _draggedCharacterIndex)
		{
			return;
		}

		_dropTargetIndex = targetIndex;
		ShowInsertionIndicator(insertionSlot);
	}

	private int ResolveInsertionSlot(Vector2 pointerPosition)
	{
		var localPosition = _gridContainer.GetGlobalTransformWithCanvas().AffineInverse() * pointerPosition;
		return _gridContainer.GetInsertionSlot(localPosition);
	}

	private void ShowInsertionIndicator(int insertionSlot)
	{
		var anchorIndex = Math.Min(insertionSlot, _members.Count - 1);
		var rect = _gridContainer.GetItemRect(anchorIndex);
		var anchorX = insertionSlot < _members.Count ? rect.Position.X : rect.End.X;
		var gridToOverlay = _dragOverlay.GetGlobalTransformWithCanvas().AffineInverse()
			* _gridContainer.GetGlobalTransformWithCanvas();
		var top = gridToOverlay * new Vector2(anchorX, rect.Position.Y);
		var bottom = gridToOverlay * new Vector2(anchorX, rect.End.Y);
		_insertionIndicator.Position = top + new Vector2(
			-InsertionIndicatorThickness / 2f, InsertionIndicatorVerticalInset);
		_insertionIndicator.Size = new Vector2(InsertionIndicatorThickness,
			Mathf.Max(0f, bottom.Y - top.Y - InsertionIndicatorVerticalInset * 2f));
		_insertionIndicator.Visible = true;
	}
}
