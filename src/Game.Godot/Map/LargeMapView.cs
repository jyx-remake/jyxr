using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.Map;

public partial class LargeMapView : Control
{
	private const float CanvasWidth = 1920f;
	private const float CanvasHeight = 1080f;
	private const float SourceWidth = 800f;
	private const float SourceHeight = 600f;
	private const float DesignOffsetX = -15f;
	private const float DesignOffsetY = -52f;
	private const float MinimumZoom = 1f;
	private const float MaximumZoom = 3f;
	private const float MouseWheelZoomStep = 1.15f;
	private const float DragThreshold = 10f;
	private const float PinMovePixelsPerSecond = 900f;
	private const float PinMoveMinDuration = 0.25f;
	private const float PinMoveMaxDuration = 1.2f;
	private static readonly Vector2 CanvasSize = new(CanvasWidth, CanvasHeight);

	private readonly Dictionary<int, Vector2> _touches = new();
	private readonly LargeMapTransform _transform = new(CanvasSize, MinimumZoom, MaximumZoom);
	private Control _mapSurface = null!;
	private TextureRect _background = null!;
	private ColorRect _timeDim = null!;
	private Control _locations = null!;
	private Control _heroPin = null!;
	private Vector2 _heroDesignPosition;
	private bool _mousePressed;
	private bool _mouseDragging;
	private Vector2 _mousePressPosition;
	private Vector2 _previousMousePosition;
	private LargeMapMarker? _mousePressedMarker;
	private TouchGestureState _touchState;
	private Vector2 _touchPressPosition;
	private LargeMapMarker? _touchPressedMarker;
	private LargeMapMarker? _tooltipMarker;
	private LargeMapMarker? _mobileTooltipMarker;
	private Control? _mobileTooltip;
	private bool _interactionEnabled = true;

	[Export]
	public PackedScene MarkerScene { get; set; } = null!;

	[Export]
	public PackedScene TooltipScene { get; set; } = null!;

	public event Action<(string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex)>? LocationPressed;

	public bool HasBackground => _background.Texture is not null;

	public override void _Ready()
	{
		_mapSurface = GetNode<Control>("%MapSurface");
		_background = GetNode<TextureRect>("%LargeMapBackground");
		_timeDim = GetNode<ColorRect>("%LargeMapTimeDim");
		_locations = GetNode<Control>("%MapEntitySlots");
		_heroPin = GetNode<Control>("%MapPin");
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
		if (!_interactionEnabled)
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
		ResetView();
		SetBackground(result.Map.Picture);
		ClearChildren(_locations);

		foreach (var location in result.Locations)
		{
			var instance = MarkerScene.Instantiate();
			if (instance is not LargeMapMarker marker)
			{
				instance.QueueFree();
				throw new InvalidOperationException("Large-map marker scene root must be LargeMapMarker.");
			}

			var designPosition = location.Location.Position is { } position
				? ProjectToDesign(position)
				: Vector2.Zero;
			marker.Setup(location, designPosition);
			_locations.AddChild(marker);
		}

		_heroDesignPosition = result.HeroPosition is { } heroPosition
			? ProjectToDesign(heroPosition)
			: Vector2.Zero;
		ApplyVisualTransform();
	}

	public void SetTimeDim(float alpha)
	{
		_timeDim.Color = new Color(0f, 0f, 0f, alpha);
		_timeDim.Visible = HasBackground && alpha > 0f;
	}

	public async Task PlayHeroMovementAsync(MapMovementResult movement, bool animated)
	{
		ArgumentNullException.ThrowIfNull(movement);
		var from = ProjectToDesign(movement.From);
		var to = ProjectToDesign(movement.To);

		ResetInputState();
		if (!animated || from.IsEqualApprox(to))
		{
			_heroDesignPosition = to;
			ApplyHeroLayout();
			return;
		}

		_interactionEnabled = false;
		_heroDesignPosition = from;
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
				_heroDesignPosition = from.Lerp(to, progress);
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
			_heroDesignPosition = to;
			ApplyHeroLayout();
		}
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
		DismissMobileTooltip();
	}

	private void SetBackground(string? resourceId)
	{
		var texture = AssetResolver.LoadTextureResource(resourceId);
		_background.Texture = texture;
		_background.Visible = texture is not null;
		if (texture is null)
		{
			_timeDim.Hide();
		}
	}

	private void OnResized()
	{
		_transform.Resize(Size);
		ApplyVisualTransform();
	}

	private void ResetView()
	{
		_transform.Reset(Size);
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
				marker.Position = _transform.Project(marker.DesignPosition);
				marker.Scale = markerScale;
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
		_heroPin.Position = _transform.Project(_heroDesignPosition);
		_heroPin.Scale = markerScale;
	}

	private void ApplyGesture(Vector2 previousCenter, Vector2 currentCenter, float zoomFactor)
	{
		_transform.ZoomAround(zoomFactor, previousCenter, currentCenter);
		ApplyVisualTransform();
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
			if (_mousePressedMarker is null)
			{
				DismissMobileTooltip();
			}

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
			DismissMobileTooltip();
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
				if (_touchPressedMarker is null)
				{
					DismissMobileTooltip();
				}
			}
			else
			{
				_touchState = TouchGestureState.Pinching;
				_touchPressedMarker = null;
				DismissMobileTooltip();
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

		_touches.Remove(touch.Index);
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
				DismissMobileTooltip();
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

		if (!Game.IsMobilePlatform)
		{
			LocationPressed?.Invoke(location);
			return;
		}

		var tooltipText = MapEntityPresentation.BuildTooltipText(location);
		if (string.IsNullOrWhiteSpace(tooltipText))
		{
			LocationPressed?.Invoke(location);
			return;
		}

		if (_mobileTooltipMarker != marker)
		{
			ShowMobileTooltip(marker, tooltipText);
			return;
		}

		DismissMobileTooltip();
		LocationPressed?.Invoke(location);
	}

	private void ShowMobileTooltip(LargeMapMarker marker, string text)
	{
		DismissMobileTooltip();
		_mobileTooltipMarker = marker;
		var markerRect = new Rect2(marker.Position, marker.Size * marker.Scale);
		_mobileTooltip = MapEntityTooltip.Show(
			this,
			TooltipScene,
			text,
			markerRect,
			new Rect2(Vector2.Zero, Size));
	}

	private void DismissMobileTooltip()
	{
		_mobileTooltipMarker = null;
		var tooltip = _mobileTooltip;
		_mobileTooltip = null;
		if (tooltip is not null && GodotObject.IsInstanceValid(tooltip))
		{
			tooltip.QueueFree();
		}
	}

	private LargeMapMarker? FindMarkerAt(Vector2 screenPosition)
	{
		for (var index = _locations.GetChildCount() - 1; index >= 0; index--)
		{
			if (_locations.GetChild(index) is LargeMapMarker marker &&
				marker.Visible &&
				new Rect2(marker.Position, marker.Size * marker.Scale).HasPoint(screenPosition))
			{
				return marker;
			}
		}

		return null;
	}

	private static Vector2 ProjectToDesign(MapPosition sourcePosition) => new(
		sourcePosition.X / SourceWidth * CanvasWidth + DesignOffsetX,
		sourcePosition.Y / SourceHeight * CanvasHeight + DesignOffsetY);

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
