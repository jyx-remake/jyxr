using Game.Application;
using Game.Core.Model;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.Map;

public partial class MapScreen
{
	private const float LargeMapWorldWidth = 1920f;
	private const float LargeMapWorldHeight = 1080f;
	private const float LargeMapSourceWidth = 800f;
	private const float LargeMapSourceHeight = 600f;
	private const float LargeMapDesignOffsetX = -15f;
	private const float LargeMapDesignOffsetY = -52f;
	private const float LargeMapMinZoom = 1f;
	private const float LargeMapMaxZoom = 3f;
	private const float LargeMapZoomStep = 1.15f;
	private const float LargeMapPinMovePixelsPerSecond = 900f;
	private const float LargeMapPinMoveMinDuration = 0.25f;
	private const float LargeMapPinMoveMaxDuration = 1.2f;
	private static readonly Vector2 LargeMapWorldSize = new(LargeMapWorldWidth, LargeMapWorldHeight);

	private SubViewportContainer _largeMapViewportContainer = null!;
	private Control _largeMapGestureArea = null!;
	private Sprite2D _largeMapBackground = null!;
	private ColorRect _largeMapTimeDim = null!;
	private Camera2D _largeMapCamera = null!;
	private Control _cloud = null!;
	private Control _mapEntitySlots = null!;
	private Control _mapPin = null!;
	private readonly Dictionary<int, Vector2> _largeMapTouches = new();
	private bool _isDraggingLargeMap;
	private Vector2 _largeMapWorldSize = LargeMapWorldSize;
	private MapEnterResult? _currentLargeMapResult;
	private float _lastLargeMapPinchDistance;
	private bool _isPlayingLargeMapMovement;
	private bool _isDeferringLargeMapTimeLighting;
	private bool _hasDeferredLargeMapTimeLighting;

	private void InitializeLargeMapNodes()
	{
		_largeMapViewportContainer = GetNode<SubViewportContainer>("%LargeMapViewportContainer");
		_largeMapGestureArea = GetNode<Control>("%LargeMapGestureArea");
		_largeMapBackground = GetNode<Sprite2D>("%LargeMapBackground");
		_largeMapTimeDim = GetNode<ColorRect>("%LargeMapTimeDim");
		_largeMapCamera = GetNode<Camera2D>("%LargeMapCamera");
		_cloud = GetNode<Control>("%Cloud");
		_mapEntitySlots = GetNode<Control>("%MapEntitySlots");
		_mapPin = GetNode<Control>("%MapPin");
		_largeMapGestureArea.GuiInput += HandleLargeMapGuiInput;
		_mapBigTab.Resized += UpdateLargeMapDisplayTransform;
		UpdateLargeMapDisplayTransform();
	}

	private void FillLargeMap(MapEnterResult result)
	{
		_currentLargeMapResult = result;
		ResizeLargeMapWorld();
		ResetLargeMapCamera();
		SetLargeMapBackground(result.Map.Picture);
		ApplyLargeMapTimeLighting();
		ClearChildren(_mapEntitySlots);

		foreach (var location in result.Locations)
		{
			var button = CreateEntityButton(MapEntitySlotScene, location);
			if (location.Location.Position is { } position)
			{
				button.Position = MapSourceToWorldPosition(position);
			}

			_mapEntitySlots.AddChild(button);
		}

		if (result.HeroPosition is { } heroPosition)
		{
			_mapPin.Position = MapSourceToWorldPosition(heroPosition);
		}
	}

	private void SetLargeMapBackground(string? resourceId)
	{
		var texture = AssetResolver.LoadTextureResource(resourceId);
		_largeMapBackground.Texture = texture;
		_largeMapBackground.Visible = texture is not null;

		if (texture is null)
		{
			_largeMapTimeDim.Hide();
			return;
		}

		var textureSize = texture.GetSize();
		if (textureSize.X <= 0f || textureSize.Y <= 0f)
		{
			_largeMapBackground.Scale = Vector2.One;
			return;
		}

		_largeMapBackground.Position = Vector2.Zero;
		ResizeLargeMapBackground();
	}

	private void ApplyLargeMapTimeLighting()
	{
		if (TryDeferLargeMapTimeLighting())
		{
			return;
		}

		ApplyLargeMapTimeLightingNow();
	}

	private void ApplyLargeMapTimeLightingNow()
	{
		if (!_largeMapBackground.Visible || _largeMapBackground.Texture is null)
		{
			_largeMapTimeDim.Hide();
			return;
		}

		var dimAlpha = MapTimeLighting.GetDimAlpha(Game.State.Clock.TimeSlot);
		_largeMapTimeDim.Color = new Color(0f, 0f, 0f, dimAlpha);
		_largeMapTimeDim.Visible = dimAlpha > 0f;
	}

	private void UpdateLargeMapDisplayTransform()
	{
		ResizeLargeMapWorld();
		LayoutLargeMapWorld();
		ClampLargeMapCamera();
	}

	private void ResizeLargeMapWorld()
	{
		var containerSize = _largeMapViewportContainer.Size;
		var parentSize = containerSize.X > 0f && containerSize.Y > 0f ? containerSize : _mapBigTab.Size;
		_largeMapWorldSize = parentSize.X > 0f && parentSize.Y > 0f ? parentSize : LargeMapWorldSize;
	}

	private void LayoutLargeMapWorld()
	{
		_largeMapGestureArea.Position = Vector2.Zero;
		_largeMapGestureArea.Size = _largeMapWorldSize;
		_cloud.Position = Vector2.Zero;
		_cloud.Size = _largeMapWorldSize;
		_largeMapTimeDim.Position = Vector2.Zero;
		_largeMapTimeDim.Size = _largeMapWorldSize;
		ResizeLargeMapBackground();
		UpdateLargeMapPositions();
		ApplyLargeMapTimeLighting();
	}

	private void ResizeLargeMapBackground()
	{
		var texture = _largeMapBackground.Texture;
		if (texture is null)
		{
			return;
		}

		var textureSize = texture.GetSize();
		if (textureSize.X <= 0f || textureSize.Y <= 0f)
		{
			_largeMapBackground.Scale = Vector2.One;
			return;
		}

		_largeMapBackground.Position = Vector2.Zero;
		_largeMapBackground.Scale = new Vector2(_largeMapWorldSize.X / textureSize.X, _largeMapWorldSize.Y / textureSize.Y);
	}

	private void UpdateLargeMapPositions()
	{
		if (_currentLargeMapResult is null)
		{
			return;
		}

		var childIndex = 0;
		foreach (var location in _currentLargeMapResult.Locations)
		{
			if (location.Location.Position is { } position &&
				childIndex < _mapEntitySlots.GetChildCount() &&
				_mapEntitySlots.GetChild(childIndex) is Control control)
			{
				control.Position = MapSourceToWorldPosition(position);
			}

			childIndex++;
		}

		if (_currentLargeMapResult.HeroPosition is { } heroPosition)
		{
			_mapPin.Position = MapSourceToWorldPosition(heroPosition);
		}
	}

	private Vector2 MapSourceToWorldPosition(MapPosition sourcePosition)
	{
		var designPosition = new Vector2(
			sourcePosition.X / LargeMapSourceWidth * LargeMapWorldWidth + LargeMapDesignOffsetX,
			sourcePosition.Y / LargeMapSourceHeight * LargeMapWorldHeight + LargeMapDesignOffsetY);

		return new Vector2(
			designPosition.X / LargeMapWorldWidth * _largeMapWorldSize.X,
			designPosition.Y / LargeMapWorldHeight * _largeMapWorldSize.Y);
	}

	private void HandleLargeMapGuiInput(InputEvent @event)
	{
		if (!_mapBigTab.Visible || _isStoryPresentationActive || _isPlayingLargeMapMovement)
		{
			ResetLargeMapInputState();
			return;
		}

		if (@event is InputEventScreenTouch screenTouch)
		{
			HandleLargeMapScreenTouch(screenTouch, screenTouch.Position);
			return;
		}

		if (@event is InputEventScreenDrag screenDrag)
		{
			HandleLargeMapScreenDrag(screenDrag, screenDrag.Position);
			return;
		}

		if (@event is InputEventMouseButton mouseButton)
		{
			HandleLargeMapMouseButton(mouseButton, mouseButton.Position);
			return;
		}

		if (@event is InputEventMouseMotion mouseMotion)
		{
			HandleLargeMapMouseMotion(mouseMotion, mouseMotion.Position);
		}
	}

	private void HandleLargeMapScreenTouch(InputEventScreenTouch screenTouch, Vector2 viewportPosition)
	{
		if (screenTouch.Pressed)
		{
			if (!IsInsideLargeMapViewport(viewportPosition))
			{
				return;
			}

			_largeMapTouches[screenTouch.Index] = viewportPosition;
			if (_largeMapTouches.Count == 1)
			{
				return;
			}

			_isDraggingLargeMap = false;
			_lastLargeMapPinchDistance = GetLargeMapTouchDistance();
			GetViewport().SetInputAsHandled();
			return;
		}

		var wasPinching = _largeMapTouches.Count > 1;
		_largeMapTouches.Remove(screenTouch.Index);
		_lastLargeMapPinchDistance = _largeMapTouches.Count > 1 ? GetLargeMapTouchDistance() : 0f;

		if (wasPinching)
		{
			_isDraggingLargeMap = false;
			GetViewport().SetInputAsHandled();
		}
	}

	private void HandleLargeMapScreenDrag(InputEventScreenDrag screenDrag, Vector2 viewportPosition)
	{
		if (!_largeMapTouches.ContainsKey(screenDrag.Index))
		{
			return;
		}

		_largeMapTouches[screenDrag.Index] = viewportPosition;
		if (_largeMapTouches.Count < 2)
		{
			return;
		}

		var pinchDistance = GetLargeMapTouchDistance();
		if (_lastLargeMapPinchDistance > 0f && pinchDistance > 0f)
		{
			ZoomLargeMap(pinchDistance / _lastLargeMapPinchDistance, GetLargeMapTouchCenter());
		}

		_lastLargeMapPinchDistance = pinchDistance;
		GetViewport().SetInputAsHandled();
	}

	private void HandleLargeMapMouseButton(InputEventMouseButton mouseButton, Vector2 viewportPosition)
	{
		var isInsideLargeMap = IsInsideLargeMapViewport(viewportPosition);

		if (mouseButton.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
		{
			if (!isInsideLargeMap)
			{
				return;
			}

			var zoomFactor = mouseButton.ButtonIndex == MouseButton.WheelUp
				? LargeMapZoomStep
				: 1f / LargeMapZoomStep;
			ZoomLargeMap(zoomFactor, viewportPosition);
			GetViewport().SetInputAsHandled();
			return;
		}

		if (mouseButton.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		if (mouseButton.Pressed)
		{
			_isDraggingLargeMap =
				_largeMapTouches.Count < 2 &&
				isInsideLargeMap &&
				!IsPointerOverLargeMapInteractive(viewportPosition);
			if (_isDraggingLargeMap)
			{
				GetViewport().SetInputAsHandled();
			}

			return;
		}

		if (_isDraggingLargeMap)
		{
			_isDraggingLargeMap = false;
			_lastLargeMapPinchDistance = 0f;
			GetViewport().SetInputAsHandled();
		}
	}

	private void HandleLargeMapMouseMotion(InputEventMouseMotion mouseMotion, Vector2 viewportPosition)
	{
		if (!_isDraggingLargeMap || _largeMapTouches.Count > 1)
		{
			return;
		}

		PanLargeMap(viewportPosition, viewportPosition - mouseMotion.Relative);
		GetViewport().SetInputAsHandled();
	}

	private void PanLargeMap(Vector2 currentViewportPosition, Vector2 previousViewportPosition)
	{
		var zoom = MathF.Max(_largeMapCamera.Zoom.X, LargeMapMinZoom);
		_largeMapCamera.Position -= (currentViewportPosition - previousViewportPosition) / zoom;
		ClampLargeMapCamera();
	}

	private void ZoomLargeMap(float zoomFactor, Vector2 viewportPosition)
	{
		var worldPositionBeforeZoom = LargeMapViewportToWorldPosition(viewportPosition);
		var zoom = Mathf.Clamp(_largeMapCamera.Zoom.X * zoomFactor, LargeMapMinZoom, LargeMapMaxZoom);
		_largeMapCamera.Zoom = new Vector2(zoom, zoom);
		var worldPositionAfterZoom = LargeMapViewportToWorldPosition(viewportPosition);
		_largeMapCamera.Position += worldPositionBeforeZoom - worldPositionAfterZoom;
		ClampLargeMapCamera();
	}

	private bool IsInsideLargeMapViewport(Vector2 viewportPosition) =>
		new Rect2(Vector2.Zero, _largeMapWorldSize).HasPoint(viewportPosition);

	private bool IsPointerOverLargeMapInteractive(Vector2 viewportPosition)
	{
		var worldPosition = LargeMapViewportToWorldPosition(viewportPosition);

		foreach (var child in _mapEntitySlots.GetChildren())
		{
			if (child is Control control &&
				control.Visible &&
				new Rect2(control.Position, control.Size).HasPoint(worldPosition))
			{
				return true;
			}
		}

		return _mapPin.Visible && new Rect2(_mapPin.Position, _mapPin.Size).HasPoint(worldPosition);
	}

	private Vector2 LargeMapViewportToWorldPosition(Vector2 viewportPosition) =>
		_largeMapCamera.Position + (viewportPosition - _largeMapWorldSize * 0.5f) / _largeMapCamera.Zoom.X;

	private void ResetLargeMapCamera()
	{
		ResetLargeMapInputState();
		_largeMapCamera.Position = _largeMapWorldSize * 0.5f;
		_largeMapCamera.Zoom = Vector2.One;
	}

	private void ResetLargeMapInputState()
	{
		_largeMapTouches.Clear();
		_isDraggingLargeMap = false;
		_lastLargeMapPinchDistance = 0f;
	}

	private void ClampLargeMapCamera()
	{
		var visibleSize = _largeMapWorldSize / _largeMapCamera.Zoom.X;
		_largeMapCamera.Position = new Vector2(
			ClampCameraAxis(_largeMapCamera.Position.X, visibleSize.X, _largeMapWorldSize.X),
			ClampCameraAxis(_largeMapCamera.Position.Y, visibleSize.Y, _largeMapWorldSize.Y));
	}

	private static float ClampCameraAxis(float value, float visibleLength, float worldLength)
	{
		if (visibleLength >= worldLength)
		{
			return worldLength * 0.5f;
		}

		var halfVisibleLength = visibleLength * 0.5f;
		return Mathf.Clamp(value, halfVisibleLength, worldLength - halfVisibleLength);
	}

	private float GetLargeMapTouchDistance()
	{
		return TryGetLargeMapTouchPair(out var first, out var second)
			? first.DistanceTo(second)
			: 0f;
	}

	private Vector2 GetLargeMapTouchCenter()
	{
		return TryGetLargeMapTouchPair(out var first, out var second)
			? (first + second) * 0.5f
			: Vector2.Zero;
	}

	private bool TryGetLargeMapTouchPair(out Vector2 first, out Vector2 second)
	{
		first = Vector2.Zero;
		second = Vector2.Zero;
		var index = 0;

		foreach (var touchPosition in _largeMapTouches.Values)
		{
			if (index == 0)
			{
				first = touchPosition;
			}
			else
			{
				second = touchPosition;
				return true;
			}

			index++;
		}

		return false;
	}

	private void BeginLargeMapTimeLightingDeferral()
	{
		if (!_mapBigTab.Visible)
		{
			return;
		}

		_isDeferringLargeMapTimeLighting = true;
		_hasDeferredLargeMapTimeLighting = false;
	}

	private void EndLargeMapTimeLightingDeferral()
	{
		if (!_isDeferringLargeMapTimeLighting)
		{
			return;
		}

		var shouldApply = _hasDeferredLargeMapTimeLighting;
		_isDeferringLargeMapTimeLighting = false;
		_hasDeferredLargeMapTimeLighting = false;

		if (shouldApply)
		{
			ApplyLargeMapTimeLightingNow();
		}
	}

	private bool TryDeferLargeMapTimeLighting()
	{
		if (!_isDeferringLargeMapTimeLighting)
		{
			return false;
		}

		_hasDeferredLargeMapTimeLighting = true;
		return true;
	}

	private async Task PlayLargeMapInteractionMovementAsync(MapMovementResult? movement)
	{
		try
		{
			await PlayLargeMapPinMoveAsync(movement);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(this))
			{
				EndLargeMapTimeLightingDeferral();
			}
		}
	}

	private async Task PlayLargeMapPinMoveAsync(MapMovementResult? movement)
	{
		if (movement is null ||
			!_mapBigTab.Visible ||
			_currentLargeMapResult is null ||
			!string.Equals(_currentLargeMapResult.Map.Id, movement.MapId, StringComparison.Ordinal))
		{
			return;
		}

		var from = MapSourceToWorldPosition(movement.From);
		var to = MapSourceToWorldPosition(movement.To);
		if (from.IsEqualApprox(to))
		{
			_mapPin.Position = to;
			_currentLargeMapResult = _currentLargeMapResult with { HeroPosition = movement.To };
			return;
		}

		if (!Game.Settings.LargeMapMovementAnimationEnabled)
		{
			ResetLargeMapInputState();
			_mapPin.Position = to;
			_currentLargeMapResult = _currentLargeMapResult with { HeroPosition = movement.To };
			return;
		}

		ResetLargeMapInputState();
		_isPlayingLargeMapMovement = true;
		_mapPin.Position = from;

		var duration = Mathf.Clamp(
			from.DistanceTo(to) / LargeMapPinMovePixelsPerSecond,
			LargeMapPinMoveMinDuration,
			LargeMapPinMoveMaxDuration);
		var positionTween = CreateTween();
		positionTween.TweenProperty(_mapPin, "position", to, duration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		try
		{
			await ToSignal(positionTween, Tween.SignalName.Finished);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(this))
			{
				_isPlayingLargeMapMovement = false;
			}
		}

		if (!GodotObject.IsInstanceValid(this))
		{
			return;
		}

		_mapPin.Position = to;
		_currentLargeMapResult = _currentLargeMapResult with { HeroPosition = movement.To };
	}
}
