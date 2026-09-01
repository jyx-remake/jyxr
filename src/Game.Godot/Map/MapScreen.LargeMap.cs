using Game.Application;
using Godot;

namespace Game.Godot.Map;

public partial class MapScreen
{
	private LargeMapView _largeMapView = null!;
	private TextureRect _largeMapCloud = null!;
	private MapEnterResult? _currentLargeMapResult;
	private bool _isDeferringLargeMapTimeLighting;
	private bool _hasDeferredLargeMapTimeLighting;
	private string? _worldBackdropId;

	private void InitializeLargeMapNodes()
	{
		_largeMapView = GetNode<LargeMapView>("%LargeMapView");
		_largeMapCloud = GetNode<TextureRect>("%Cloud");
		_largeMapView.LocationPressed += _locationTooltipLayer.Request;
		_largeMapView.GestureStarted += _locationTooltipLayer.Dismiss;
		_worldBackdropId = World.Instance.CurrentBackdropId;
		World.Instance.BackdropChanged += OnWorldBackdropChanged;
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
