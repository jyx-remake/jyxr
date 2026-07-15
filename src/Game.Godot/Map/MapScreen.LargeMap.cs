using Game.Application;
using Godot;

namespace Game.Godot.Map;

public partial class MapScreen
{
	private LargeMapView _largeMapView = null!;
	private MapEnterResult? _currentLargeMapResult;
	private bool _isDeferringLargeMapTimeLighting;
	private bool _hasDeferredLargeMapTimeLighting;

	private void InitializeLargeMapNodes()
	{
		_largeMapView = GetNode<LargeMapView>("%LargeMapView");
		_largeMapView.LocationPressed += OnLocationPressed;
	}

	private void FillLargeMap(MapEnterResult result)
	{
		_currentLargeMapResult = result;
		_largeMapView.ShowMap(result);
		ApplyLargeMapTimeLighting();
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
