using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.Map;

public partial class LargeMapView : Control
{
	private const float MinimumZoom = 1f;
	private const float MaximumZoom = 3f;
	private const float MobileDefaultZoom = 1.5f;
	private const float MouseWheelZoomStep = 1.15f;
	private const double ZoomSaveDelaySeconds = 1.0;
	private const float DragThreshold = 10f;
	private const float PinMovePixelsPerSecond = 900f;
	private const float PinMoveMinDuration = 0.25f;
	private const float PinMoveMaxDuration = 1.2f;
	private static readonly Vector2 CanvasSize = new(
		LargeMapCoordinateSpace.Width,
		LargeMapCoordinateSpace.Height);

	private readonly Dictionary<int, Vector2> _touches = new();
	private readonly LargeMapTransform _transform = new(CanvasSize, MinimumZoom, MaximumZoom);
	private Control _mapSurface = null!;
	private TextureRect _background = null!;
	private ColorRect _timeDim = null!;
	private Control _locations = null!;
	private Control _heroPin = null!;
	private TextureRect _heroAvatar = null!;
	private global::Godot.Timer _zoomSaveTimer = null!;
	private float _savedZoom;
	private Vector2 _heroLogicalPosition;
	private bool _mousePressed;
	private bool _mouseDragging;
	private Vector2 _mousePressPosition;
	private Vector2 _previousMousePosition;
	private LargeMapMarker? _mousePressedMarker;
	private TouchGestureState _touchState;
	private Vector2 _touchPressPosition;
	private LargeMapMarker? _touchPressedMarker;
	private LargeMapMarker? _tooltipMarker;
	private bool _interactionEnabled = true;
	private bool _inputSuppressed;

	[Export]
	public PackedScene MarkerScene { get; set; } = null!;

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	public event Action<
		(string MapId, MapLocationDefinition Location, MapEventDefinition? Event),
		Rect2>? LocationPressed;
	public event Action? GestureStarted;

	public bool HasBackground => _background.Texture is not null;

	public override void _Ready()
	{
		_mapSurface = GetNode<Control>("%MapSurface");
		_background = GetNode<TextureRect>("%LargeMapBackground");
		_timeDim = GetNode<ColorRect>("%LargeMapTimeDim");
		_locations = GetNode<Control>("%MapEntitySlots");
		_heroPin = GetNode<Control>("%MapPin");
		_heroAvatar = GetNode<TextureRect>("%PinAvatar");
		_mapSurface.Size = CanvasSize;
		_savedZoom = Game.UserSettings.Current.LargeMapZoom;
		_zoomSaveTimer = new global::Godot.Timer
		{
			OneShot = true,
			WaitTime = ZoomSaveDelaySeconds,
			// 与 PlayTimeCoordinator 一致：剧情或 Mod 指令可能暂停整棵树，
			// 用 Always 保证缩放保存定时器不会因此停摆，避免缩放变化丢失。
			ProcessMode = ProcessModeEnum.Always,
		};
		_zoomSaveTimer.Timeout += PersistZoom;
		AddChild(_zoomSaveTimer);
		Resized += OnResized;
		ResetView();
	}

	public override string _GetTooltip(Vector2 atPosition)
	{
		_tooltipMarker = FindMarkerAt(atPosition);
		return _tooltipMarker?.Location is { } location
			? MapEntityPresentation.BuildTooltipText(location)
			: string.Empty;
	}

	public override Control? _MakeCustomTooltip(string forText) =>
		_tooltipMarker is not null &&
		GodotObject.IsInstanceValid(_tooltipMarker) &&
		!string.IsNullOrWhiteSpace(forText)
			? MapEntityTooltip.Create(TooltipScene, forText)
			: null;

	public override void _GuiInput(InputEvent @event)
	{
		if (!_interactionEnabled || _inputSuppressed)
		{
			return;
		}

		var handled = @event switch
		{
			InputEventScreenTouch touch => HandleTouch(touch),
			InputEventScreenDrag drag => HandleTouchDrag(drag),
			InputEventMouseButton mouseButton => HandleMouseButton(mouseButton),
			InputEventMouseMotion mouseMotion => HandleMouseMotion(mouseMotion),
			_ => false,
		};

		if (handled)
		{
			AcceptEvent();
		}
	}

	public void ShowMap(MapEnterResult result)
	{
		ArgumentNullException.ThrowIfNull(result);
		if (MarkerScene is null)
		{
			throw new InvalidOperationException("Large-map marker scene is not assigned.");
		}

		ResetInputState();
		SetBackground(result.Map.Picture);
		SetHeroPortrait();
		ClearChildren(_locations);

		foreach (var location in result.Locations)
		{
			var instance = MarkerScene.Instantiate();
			if (instance is not LargeMapMarker marker)
			{
				instance.QueueFree();
				throw new InvalidOperationException("Large-map marker scene root must be LargeMapMarker.");
			}

			var logicalPosition = location.Location.Position is { } position
				? new Vector2(position.X, position.Y)
				: Vector2.Zero;
			marker.Setup(location, logicalPosition);
			_locations.AddChild(marker);
		}

		Vector2? heroLogicalPosition = result.HeroPosition is { } heroPosition
			? new Vector2(heroPosition.X, heroPosition.Y)
			: null;
		_heroLogicalPosition = heroLogicalPosition ?? Vector2.Zero;
		ResetView(heroLogicalPosition);
	}

	public void SetTimeDim(float alpha)
	{
		_timeDim.Color = new Color(0f, 0f, 0f, alpha);
		_timeDim.Visible = HasBackground && alpha > 0f;
	}

	public async Task PlayHeroMovementAsync(MapMovementResult movement, bool animated)
	{
		ArgumentNullException.ThrowIfNull(movement);
		var from = new Vector2(movement.From.X, movement.From.Y);
		var to = new Vector2(movement.To.X, movement.To.Y);

		ResetInputState();
		if (!animated || from.IsEqualApprox(to))
		{
			_heroLogicalPosition = to;
			ApplyHeroLayout();
			return;
		}

		_interactionEnabled = false;
		_heroLogicalPosition = from;
		ApplyHeroLayout();
		var screenDistance = _transform.Project(from).DistanceTo(_transform.Project(to));
		var duration = Mathf.Clamp(
			screenDistance / PinMovePixelsPerSecond,
			PinMoveMinDuration,
			PinMoveMaxDuration);
		var tween = CreateTween();
		tween.TweenMethod(
			Callable.From<float>(progress =>
			{
				_heroLogicalPosition = from.Lerp(to, progress);
				ApplyHeroLayout();
			}),
			0f,
			1f,
			duration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		try
		{
			await ToSignal(tween, Tween.SignalName.Finished);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(this))
			{
				_interactionEnabled = true;
			}
		}

		if (GodotObject.IsInstanceValid(this))
		{
			_heroLogicalPosition = to;
			ApplyHeroLayout();
		}
	}

	/// <summary>
	/// Enables or disables gesture/click handling. Kept separate from the
	/// transient lock used by the hero movement animation so the two never
	/// re-enable each other. The map stays visible while a story is presenting,
	/// so its input must be suppressed instead of relying on being hidden.
	/// </summary>
	public void SetInteractionEnabled(bool enabled)
	{
		if (!enabled)
		{
			ResetInputState();
		}

		_inputSuppressed = !enabled;
	}

	public void ResetInputState()
	{
		_mousePressed = false;
		_mouseDragging = false;
		_mousePressedMarker = null;
		_touches.Clear();
		_touchState = TouchGestureState.Idle;
		_touchPressedMarker = null;
		_tooltipMarker = null;
	}

	private void SetBackground(string? resourceId)
	{
		var texture = AssetResolver.LoadTexture(resourceId);
		_background.Texture = texture;
		_background.Visible = texture is not null;
		if (texture is null)
		{
			_timeDim.Hide();
		}
	}

	private void SetHeroPortrait()
	{
		var hero = Game.State.Party.GetMember(Party.HeroCharacterId);
		_heroAvatar.Texture = AssetResolver.LoadTexture(hero.Portrait);
	}

	private void OnResized()
	{
		_transform.Resize(Size);
		ApplyVisualTransform();
	}

	private void ResetView(Vector2? centerLogicalPosition = null)
	{
		var initialZoom = _savedZoom > 0f
			? _savedZoom
			: Game.IsMobilePlatform ? MobileDefaultZoom : MinimumZoom;
		_transform.Reset(Size, initialZoom, centerLogicalPosition);
		ApplyVisualTransform();
	}

	private void ApplyVisualTransform()
	{
		if (!IsInsideTree())
		{
			return;
		}

		_mapSurface.Position = _transform.Translation;
		_mapSurface.Scale = _transform.SurfaceScale;
		var markerScale = new Vector2(_transform.MarkerScale, _transform.MarkerScale);
		foreach (var child in _locations.GetChildren())
		{
			if (child is LargeMapMarker marker)
			{
				marker.Position = _transform.Project(marker.LogicalPosition);
				// marker 自身只按 markerScale 缩放（不影响 label/notice 字号）
				// IconScaleFactor 在 SetIconScale 中仅作用于 Avatar/OverflowAvatar
				marker.Scale = markerScale;
				marker.SetIconScale(markerScale);
			}
		}

		ApplyHeroLayout(markerScale);
	}

	private void ApplyHeroLayout()
	{
		var markerScale = new Vector2(_transform.MarkerScale, _transform.MarkerScale);
		ApplyHeroLayout(markerScale);
	}

	private void ApplyHeroLayout(Vector2 markerScale)
	{
		_heroPin.Position = _transform.Project(_heroLogicalPosition);
		// 主角头像跟随 markerScale 一起缩放，保持相对底图比例恒定。
		_heroPin.Scale = markerScale;
	}

	private void ApplyGesture(Vector2 previousCenter, Vector2 currentCenter, float zoomFactor)
	{
		_transform.ZoomAround(zoomFactor, previousCenter, currentCenter);
		ApplyVisualTransform();
	}

	private void ScheduleZoomSave()
	{
		if (Mathf.IsEqualApprox(_savedZoom, _transform.Zoom))
		{
			return;
		}

		_zoomSaveTimer.Start();
	}

	private void PersistZoom()
	{
		try
		{
			var zoom = _transform.Zoom;
			Game.UserSettings.Update(settings => settings with { LargeMapZoom = zoom });
			_savedZoom = zoom;
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Failed to save large-map zoom.", exception);
		}
	}

	/// <summary>
	/// Immediately persists a pending zoom change instead of waiting for the
	/// delayed save timer. Called before the view is rebuilt so the replacement
	/// map restores the zoom the player currently sees.
	/// </summary>
	public void FlushPendingZoomSave()
	{
		if (_zoomSaveTimer.TimeLeft <= 0d)
		{
			return;
		}

		_zoomSaveTimer.Stop();
		PersistZoom();
	}

	private bool HandleMouseButton(InputEventMouseButton mouseButton)
	{
		var position = mouseButton.Position;
		if (mouseButton.Device == InputEvent.DeviceIdEmulation)
		{
			return true;
		}

		if (mouseButton.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
		{
			if (!mouseButton.Pressed)
			{
				return true;
			}

			var factor = mouseButton.ButtonIndex == MouseButton.WheelUp
				? MouseWheelZoomStep
				: 1f / MouseWheelZoomStep;
			ApplyGesture(position, position, factor);
			ScheduleZoomSave();
			return true;
		}

		if (mouseButton.ButtonIndex != MouseButton.Left)
		{
			return false;
		}

		if (mouseButton.Pressed)
		{
			_mousePressed = true;
			_mouseDragging = false;
			_mousePressPosition = position;
			_previousMousePosition = position;
			_mousePressedMarker = FindMarkerAt(position);

			return true;
		}

		if (!_mousePressed)
		{
			return false;
		}

		if (!_mouseDragging && _mousePressedMarker is { } marker && marker == FindMarkerAt(position))
		{
			ActivateMarker(marker);
		}

		_mousePressed = false;
		_mouseDragging = false;
		_mousePressedMarker = null;
		return true;
	}

	private bool HandleMouseMotion(InputEventMouseMotion mouseMotion)
	{
		if (mouseMotion.Device == InputEvent.DeviceIdEmulation)
		{
			return _touches.Count > 0;
		}

		if (!_mousePressed)
		{
			return false;
		}

		var position = mouseMotion.Position;
		if (!_mouseDragging && position.DistanceTo(_mousePressPosition) >= DragThreshold)
		{
			_mouseDragging = true;
			_mousePressedMarker = null;
			GestureStarted?.Invoke();
		}

		if (_mouseDragging)
		{
			_transform.Pan(position - _previousMousePosition);
			ApplyVisualTransform();
		}

		_previousMousePosition = position;
		return true;
	}

	private bool HandleTouch(InputEventScreenTouch touch)
	{
		var position = touch.Position;
		if (touch.Pressed)
		{
			if (_touches.Count >= 2)
			{
				return true;
			}

			_touches[touch.Index] = position;
			if (_touches.Count == 1)
			{
				_touchState = TouchGestureState.PendingTap;
				_touchPressPosition = position;
				_touchPressedMarker = FindMarkerAt(position);
			}
			else
			{
				_touchState = TouchGestureState.Pinching;
				_touchPressedMarker = null;
				GestureStarted?.Invoke();
			}
			return true;
		}

		if (!_touches.ContainsKey(touch.Index))
		{
			return false;
		}

		var releasedMarker = FindMarkerAt(position);
		var shouldActivate =
			!touch.Canceled &&
			_touches.Count == 1 &&
			_touchState == TouchGestureState.PendingTap &&
			position.DistanceTo(_touchPressPosition) < DragThreshold &&
			_touchPressedMarker is not null &&
			_touchPressedMarker == releasedMarker;

		var wasPinching = _touchState == TouchGestureState.Pinching;
		_touches.Remove(touch.Index);
		if (wasPinching)
		{
			ScheduleZoomSave();
		}
		if (shouldActivate)
		{
			ActivateMarker(_touchPressedMarker!);
		}

		if (_touches.Count == 0)
		{
			_touchState = TouchGestureState.Idle;
			_touchPressedMarker = null;
		}
		else
		{
			_touchState = TouchGestureState.Dragging;
		}

		return true;
	}

	private bool HandleTouchDrag(InputEventScreenDrag drag)
	{
		if (!_touches.TryGetValue(drag.Index, out var previousPosition))
		{
			return false;
		}

		var position = drag.Position;
		if (_touches.Count == 1)
		{
			_touches[drag.Index] = position;
			if (_touchState == TouchGestureState.PendingTap && position.DistanceTo(_touchPressPosition) >= DragThreshold)
			{
				_touchState = TouchGestureState.Dragging;
				_touchPressedMarker = null;
				GestureStarted?.Invoke();
			}

			if (_touchState == TouchGestureState.Dragging)
			{
				_transform.Pan(position - previousPosition);
				ApplyVisualTransform();
			}

			return true;
		}

		if (TryGetOtherTouch(drag.Index, out var otherPosition))
		{
			if (_touchState != TouchGestureState.Pinching)
			{
				GestureStarted?.Invoke();
			}

			_touches[drag.Index] = position;
			_touchState = TouchGestureState.Pinching;
			_touchPressedMarker = null;
			var previousCenter = (previousPosition + otherPosition) * 0.5f;
			var currentCenter = (position + otherPosition) * 0.5f;
			var previousDistance = previousPosition.DistanceTo(otherPosition);
			var currentDistance = position.DistanceTo(otherPosition);
			var factor = previousDistance > 0f ? currentDistance / previousDistance : 1f;
			ApplyGesture(previousCenter, currentCenter, factor);
		}

		return true;
	}

	private bool TryGetOtherTouch(int touchIndex, out Vector2 position)
	{
		foreach (var touch in _touches)
		{
			if (touch.Key != touchIndex)
			{
				position = touch.Value;
				return true;
			}
		}

		position = default;
		return false;
	}

	private void ActivateMarker(LargeMapMarker marker)
	{
		if (marker.Location is not { } location || location.Event is null)
		{
			return;
		}

		var localBounds = marker.GetScreenBounds();
		var canvasTransform = GetGlobalTransformWithCanvas();
		var firstCorner = canvasTransform * localBounds.Position;
		var secondCorner = canvasTransform * localBounds.End;
		LocationPressed?.Invoke(location, new Rect2(firstCorner, secondCorner - firstCorner).Abs());
	}

	private LargeMapMarker? FindMarkerAt(Vector2 screenPosition)
	{
		for (var index = _locations.GetChildCount() - 1; index >= 0; index--)
		{
			if (_locations.GetChild(index) is LargeMapMarker marker &&
				marker.Visible &&
				marker.GetScreenBounds().HasPoint(screenPosition))
			{
				return marker;
			}
		}

		return null;
	}

	private static void ClearChildren(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

	private enum TouchGestureState
	{
		Idle,
		PendingTap,
		Dragging,
		Pinching,
	}
}
