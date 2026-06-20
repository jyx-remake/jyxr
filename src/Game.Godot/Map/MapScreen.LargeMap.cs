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
	private const float LargeMapMinZoom = 1f;
	private const float LargeMapMaxZoom = 3f;
	private const float LargeMapZoomStep = 1.15f;
	private static readonly Vector2 LargeMapWorldSize = new(LargeMapWorldWidth, LargeMapWorldHeight);

	private SubViewportContainer _largeMapViewportContainer = null!;
	private Sprite2D _largeMapBackground = null!;
	private Camera2D _largeMapCamera = null!;
	private Control _cloud = null!;
	private Control _mapEntitySlots = null!;
	private Control _mapPin = null!;
	private readonly Dictionary<int, Vector2> _largeMapTouches = new();
	private bool _isDraggingLargeMap;
	private Vector2 _largeMapWorldSize = LargeMapWorldSize;
	private MapEnterResult? _currentLargeMapResult;
	private float _lastLargeMapPinchDistance;

	private void InitializeLargeMapNodes()
	{
		_largeMapViewportContainer = GetNode<SubViewportContainer>("%LargeMapViewportContainer");
		_largeMapBackground = GetNode<Sprite2D>("%LargeMapBackground");
		_largeMapCamera = GetNode<Camera2D>("%LargeMapCamera");
		_cloud = GetNode<Control>("%Cloud");
		_mapEntitySlots = GetNode<Control>("%MapEntitySlots");
		_mapPin = GetNode<Control>("%MapPin");
		_mapBigTab.Resized += UpdateLargeMapDisplayTransform;
		UpdateLargeMapDisplayTransform();
	}

	private void FillLargeMap(MapEnterResult result)
	{
		_currentLargeMapResult = result;
		ResizeLargeMapWorld();
		ResetLargeMapCamera();
		SetLargeMapBackground(result.Map.Picture);
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
		_cloud.Position = Vector2.Zero;
		_cloud.Size = _largeMapWorldSize;
		ResizeLargeMapBackground();
		UpdateLargeMapPositions();
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

	private Vector2 MapSourceToWorldPosition(MapPosition sourcePosition) =>
		new(
			sourcePosition.X / LargeMapSourceWidth * _largeMapWorldSize.X,
			sourcePosition.Y / LargeMapSourceHeight * _largeMapWorldSize.Y);

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_mapBigTab.Visible || _isStoryPresentationActive)
		{
			ResetLargeMapInputState();
			return;
		}

		if (@event is InputEventScreenTouch screenTouch)
		{
			HandleLargeMapScreenTouch(screenTouch);
			return;
		}

		if (@event is InputEventScreenDrag screenDrag)
		{
			HandleLargeMapScreenDrag(screenDrag);
			return;
		}

		if (@event is InputEventMouseButton mouseButton)
		{
			HandleLargeMapMouseButton(mouseButton);
			return;
		}

		if (@event is InputEventMouseMotion mouseMotion)
		{
			HandleLargeMapMouseMotion(mouseMotion);
		}
	}

	private void HandleLargeMapScreenTouch(InputEventScreenTouch screenTouch)
	{
		if (screenTouch.Pressed)
		{
			if (!IsInsideLargeMapViewport(screenTouch.Position))
			{
				return;
			}

			_largeMapTouches[screenTouch.Index] = screenTouch.Position;
			if (_largeMapTouches.Count == 1)
			{
				_isDraggingLargeMap = !IsPointerOverLargeMapInteractive(screenTouch.Position);
				if (_isDraggingLargeMap)
				{
					GetViewport().SetInputAsHandled();
				}
				return;
			}

			_isDraggingLargeMap = false;
			_lastLargeMapPinchDistance = GetLargeMapTouchDistance();
			GetViewport().SetInputAsHandled();
			return;
		}

		var wasHandlingTouch = _isDraggingLargeMap || _largeMapTouches.Count > 1;
		_largeMapTouches.Remove(screenTouch.Index);
		_isDraggingLargeMap = false;
		_lastLargeMapPinchDistance = _largeMapTouches.Count > 1 ? GetLargeMapTouchDistance() : 0f;

		if (wasHandlingTouch)
		{
			GetViewport().SetInputAsHandled();
		}
	}

	private void HandleLargeMapScreenDrag(InputEventScreenDrag screenDrag)
	{
		if (!_largeMapTouches.TryGetValue(screenDrag.Index, out var previousPosition))
		{
			return;
		}

		_largeMapTouches[screenDrag.Index] = screenDrag.Position;
		if (_largeMapTouches.Count > 1)
		{
			var pinchDistance = GetLargeMapTouchDistance();
			if (_lastLargeMapPinchDistance > 0f && pinchDistance > 0f)
			{
				ZoomLargeMap(pinchDistance / _lastLargeMapPinchDistance, GetLargeMapTouchCenter());
			}

			_lastLargeMapPinchDistance = pinchDistance;
			GetViewport().SetInputAsHandled();
			return;
		}

		if (!_isDraggingLargeMap)
		{
			return;
		}

		PanLargeMap(screenDrag.Position, previousPosition);
		GetViewport().SetInputAsHandled();
	}

	private void HandleLargeMapMouseButton(InputEventMouseButton mouseButton)
	{
		var isInsideLargeMap = IsInsideLargeMapViewport(mouseButton.Position);

		if (mouseButton.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
		{
			if (!isInsideLargeMap || IsPointerBlockedByForegroundControl())
			{
				return;
			}

			var zoomFactor = mouseButton.ButtonIndex == MouseButton.WheelUp
				? LargeMapZoomStep
				: 1f / LargeMapZoomStep;
			ZoomLargeMap(zoomFactor, mouseButton.Position);
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
				isInsideLargeMap &&
				!IsPointerBlockedByForegroundControl() &&
				!IsPointerOverLargeMapInteractive(mouseButton.Position);
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

	private void HandleLargeMapMouseMotion(InputEventMouseMotion mouseMotion)
	{
		if (!_isDraggingLargeMap)
		{
			return;
		}

		if (IsPointerBlockedByForegroundControl())
		{
			ResetLargeMapInputState();
			return;
		}

		PanLargeMap(mouseMotion.Position, mouseMotion.Position - mouseMotion.Relative);
		GetViewport().SetInputAsHandled();
	}

	private void PanLargeMap(Vector2 currentScreenPosition, Vector2 previousScreenPosition)
	{
		var currentViewportPosition = ScreenToLargeMapViewportPosition(currentScreenPosition);
		var previousViewportPosition = ScreenToLargeMapViewportPosition(previousScreenPosition);
		var zoom = MathF.Max(_largeMapCamera.Zoom.X, LargeMapMinZoom);
		_largeMapCamera.Position -= (currentViewportPosition - previousViewportPosition) / zoom;
		ClampLargeMapCamera();
	}

	private void ZoomLargeMap(float zoomFactor, Vector2 screenPosition)
	{
		var worldPositionBeforeZoom = ScreenToLargeMapWorldPosition(screenPosition);
		var zoom = Mathf.Clamp(_largeMapCamera.Zoom.X * zoomFactor, LargeMapMinZoom, LargeMapMaxZoom);
		_largeMapCamera.Zoom = new Vector2(zoom, zoom);
		var worldPositionAfterZoom = ScreenToLargeMapWorldPosition(screenPosition);
		_largeMapCamera.Position += worldPositionBeforeZoom - worldPositionAfterZoom;
		ClampLargeMapCamera();
	}

	private bool IsInsideLargeMapViewport(Vector2 screenPosition) =>
		new Rect2(Vector2.Zero, _largeMapWorldSize).HasPoint(ScreenToLargeMapViewportPosition(screenPosition));

	private bool IsPointerOverLargeMapInteractive(Vector2 screenPosition)
	{
		var worldPosition = ScreenToLargeMapWorldPosition(screenPosition);

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

	private bool IsPointerBlockedByForegroundControl()
	{
		var hoveredControl = GetViewport().GuiGetHoveredControl();
		if (hoveredControl is null)
		{
			return false;
		}

		return hoveredControl != this && !IsAncestorOf(hoveredControl);
	}

	private Vector2 ScreenToLargeMapWorldPosition(Vector2 screenPosition)
	{
		var viewportPosition = ScreenToLargeMapViewportPosition(screenPosition);
		return _largeMapCamera.Position + (viewportPosition - _largeMapWorldSize * 0.5f) / _largeMapCamera.Zoom.X;
	}

	private Vector2 ScreenToLargeMapViewportPosition(Vector2 screenPosition) =>
		_largeMapViewportContainer.GetGlobalTransformWithCanvas().AffineInverse() * screenPosition;

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
}
