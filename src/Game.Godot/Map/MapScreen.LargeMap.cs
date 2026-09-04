using Game.Application;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Map;

public partial class MapScreen
{
	private LargeMapView _largeMapView = null!;
	private Control _largeMapSafeArea = null!;
	private TextureRect _largeMapCloud = null!;
	private MapEnterResult? _currentLargeMapResult;
	private bool _isDeferringLargeMapTimeLighting;
	private bool _hasDeferredLargeMapTimeLighting;
	private string? _worldBackdropId;

	/// <summary>
	/// When enabled, the safe area's offsets are overwritten from the HUD's
	/// measured insets on every layout pass. When disabled (default), the
	/// safe-area node's editor-set offsets are used as-is so the region can
	/// be adjusted by hand.
	/// </summary>
	[Export]
	public bool SafeAreaFollowHud { get; set; }

	private void InitializeLargeMapNodes()
	{
		_largeMapView = GetNode<LargeMapView>("%LargeMapView");
		_largeMapSafeArea = GetNode<Control>("%LargeMapSafeArea");
		_largeMapCloud = GetNode<TextureRect>("%Cloud");
		_largeMapView.LocationPressed += _locationTooltipLayer.Request;
		_largeMapView.GestureStarted += _locationTooltipLayer.Dismiss;
		_worldBackdropId = World.Instance.CurrentBackdropId;
		World.Instance.BackdropChanged += OnWorldBackdropChanged;
		// Keep the map viewport inside the HUD's safe area (below the top bar
		// frame, above the bottom bar frame). Re-measured on every resize so
		// HUD tweaks follow automatically.
		Resized += UpdateLargeMapSafeArea;
		CallDeferred(nameof(UpdateLargeMapSafeArea));
	}

	/// <summary>
	/// With <see cref="SafeAreaFollowHud"/> enabled, shrinks the safe area to
	/// the band between the HUD's top bar frame and bottom bar frame. The
	/// transform clamps pan/zoom against this viewport, so with
	/// <c>clip_contents</c> on, map content can never render under the HUD
	/// strips and no black edges can appear (the map always covers the
	/// viewport at the minimum zoom). Disable it to adjust the region by hand
	/// in the scene editor.
	/// </summary>
	private void UpdateLargeMapSafeArea()
	{
		if (_isLargeMapSafeAreaExpanded || !SafeAreaFollowHud || !IsInsideTree())
		{
			return;
		}

		var hud = UIRoot.Instance.Hud;
		if (hud is null || !GodotObject.IsInstanceValid(hud))
		{
			return;
		}

		_largeMapSafeArea.OffsetTop = hud.TopSafeInset;
		_largeMapSafeArea.OffsetBottom = -hud.BottomSafeInset;
	}

	private void ReleaseLargeMapNodes()
	{
		World.Instance.BackdropChanged -= OnWorldBackdropChanged;
	}

	private void OnWorldBackdropChanged(string? resourceId)
	{
		_worldBackdropId = resourceId;
		if (GodotObject.IsInstanceValid(this) && IsInsideTree())
		{
			ApplyStoryPresentationVisibility();
		}
	}

	/// <summary>
	/// True while a story has replaced the shared world backdrop with art of its
	/// own (the `background` command). The zoomed large map must step aside for
	/// that art; when no story backdrop is present the map stays on screen so
	/// map events play at the zoom the player is currently using.
	/// </summary>
	private bool HasForeignStoryBackdrop =>
		_currentLargeMapResult is { } result &&
		!string.IsNullOrEmpty(_worldBackdropId) &&
		!string.Equals(_worldBackdropId, result.Map.Picture, StringComparison.Ordinal);

	private void FillLargeMap(MapEnterResult result)
	{
		_currentLargeMapResult = result;
		UpdateLargeMapSafeArea();
		_largeMapView.ShowMap(result);
		ApplyLargeMapCloudVisibility();
		ApplyLargeMapTimeLighting();
	}

	/// <summary>
	/// Persists the live large-map zoom before the screen is rebuilt, so the
	/// replacement view restores the zoom the player currently sees instead of
	/// the last value saved by the delayed zoom-save timer.
	/// </summary>
	public void FlushLargeMapZoom()
	{
		if (GodotObject.IsInstanceValid(_largeMapView))
		{
			_largeMapView.FlushPendingZoomSave();
		}
	}

	private void ApplyLargeMapCloudVisibility()
	{
		if (GodotObject.IsInstanceValid(_largeMapCloud))
		{
			_largeMapCloud.Visible = Game.State.Adventure.CloudVisible;
		}
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
		var dimAlpha = _largeMapView.HasBackground
			? MapTimeLighting.GetDimAlpha(Game.State.Clock.TimeSlot)
			: 0f;
		_largeMapView.SetTimeDim(dimAlpha);
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

		await _largeMapView.PlayHeroMovementAsync(
			movement,
			Game.Settings.LargeMapMovementAnimationEnabled);
		if (GodotObject.IsInstanceValid(this))
		{
			_currentLargeMapResult = _currentLargeMapResult with { HeroPosition = movement.To };
		}
	}
}
