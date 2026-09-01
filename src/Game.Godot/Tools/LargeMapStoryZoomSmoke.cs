using Game.Application.Mods;
using Game.Godot.Map;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless verification of the large-map zoom-during-story fix.
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/LargeMapStoryZoomSmoke.cs
///
/// Asserts the exact behaviour the player expects:
///  - A dialogue-only story (no `background` command, e.g. 英雄雕像剧情) keeps
///    the LargeMapView visible at the zoom/pan the player is using.
///  - A story that supplies its own backdrop (`background` command) hides the map.
///  - The zoom survives a post-story map rebuild (RefreshCurrentMap).
/// </summary>
public partial class LargeMapStoryZoomSmoke : SceneTree
{
	public override void _Initialize() => CallDeferred(nameof(RunSmoke));

	private async void RunSmoke()
	{
		try
		{
			var projectRoot = ProjectSettings.GlobalizePath("res://");
			var dataRoot = ProjectDataRoot.FromPath(projectRoot);
			var mods = new ModRegistry(dataRoot).DiscoverMods();
			var loadout = new ModLoadoutResolver(mods).Resolve("xmjh", []);
			GameRuntimeBootstrap.Initialize(loadout, this);

			await SettleFramesAsync();

			var world = World.Instance;
			world.ShowMap("大地图洛阳");
			await SettleFramesAsync();

			if (world.CurrentScene is not MapScreen mapScreen)
			{
				throw new InvalidOperationException("World.CurrentScene is not MapScreen after ShowMap.");
			}

			LargeMapView LargeMapViewOf(MapScreen screen) =>
				screen.FindChild("LargeMapView", true, false) as LargeMapView
					?? throw new InvalidOperationException("LargeMapView not found under MapScreen.");

			Control MapSurfaceOf(LargeMapView view) =>
				view.FindChild("MapSurface", true, false) as Control
					?? throw new InvalidOperationException("MapSurface not found under LargeMapView.");

			var failures = new List<string>();
			var largeMapView = LargeMapViewOf(mapScreen);
			var mapSurface = MapSurfaceOf(largeMapView);

			// --- Check 1: dialogue-only story keeps the map visible at current zoom. ---
			var visibleBefore = largeMapView.Visible;
			var scaleBefore = mapSurface.Scale;

			UIRoot.Instance.SetStoryPresentationActive(true);
			var visibleDuringDialogue = largeMapView.Visible;
			var scaleDuringDialogue = mapSurface.Scale;
			UIRoot.Instance.SetStoryPresentationActive(false);
			var visibleAfter = largeMapView.Visible;

			if (!visibleBefore) failures.Add("map hidden before story");
			if (!visibleDuringDialogue) failures.Add("map hidden during dialogue-only story");
			if (!visibleAfter) failures.Add("map hidden after story");
			if ((scaleBefore - scaleDuringDialogue).Length() > 0.001f)
				failures.Add($"map scale changed during story ({scaleBefore} -> {scaleDuringDialogue})");

			// --- Check 2: a story-provided backdrop hides the map. ---
			world.SetBackground("地图.大地图");
			UIRoot.Instance.SetStoryPresentationActive(true);
			var visibleDuringForeignBackdrop = largeMapView.Visible;
			UIRoot.Instance.SetStoryPresentationActive(false);

			if (visibleDuringForeignBackdrop) failures.Add("map not hidden during foreign-backdrop story");

			// --- Check 3: zoom survives a post-story rebuild. ---
			// Raise the saved zoom, rebuild the map (as RefreshCurrentMap does after
			// a story), then verify the rebuilt surface carries the zoom and that a
			// story presentation no longer resets it.
			Game.UserSettings.Update(settings => settings with { LargeMapZoom = 2.0f });
			world.RefreshCurrentMap();
			await SettleFramesAsync();

			mapScreen = (MapScreen)world.CurrentScene!;
			largeMapView = LargeMapViewOf(mapScreen);
			mapSurface = MapSurfaceOf(largeMapView);
			var scaleAtZoom2 = mapSurface.Scale;

			UIRoot.Instance.SetStoryPresentationActive(true);
			var scaleAtZoom2DuringStory = mapSurface.Scale;
			UIRoot.Instance.SetStoryPresentationActive(false);

			world.RefreshCurrentMap();
			await SettleFramesAsync();
			mapScreen = (MapScreen)world.CurrentScene!;
			largeMapView = LargeMapViewOf(mapScreen);
			mapSurface = MapSurfaceOf(largeMapView);
			var scaleAfterRebuild = mapSurface.Scale;

			if ((scaleAtZoom2 - scaleAtZoom2DuringStory).Length() > 0.001f)
				failures.Add($"map scale changed during story at zoom 2 ({scaleAtZoom2} -> {scaleAtZoom2DuringStory})");
			if ((scaleAtZoom2 - scaleAfterRebuild).Length() > 0.001f)
				failures.Add($"zoom lost across rebuild ({scaleAtZoom2} -> {scaleAfterRebuild})");

			GD.Print(
				$"LARGE_MAP_ZOOM_SMOKE visible before={visibleBefore} duringDialogue={visibleDuringDialogue} after={visibleAfter} " +
				$"duringForeignBackdrop={visibleDuringForeignBackdrop} scaleBefore={scaleBefore} scaleDuringDialogue={scaleDuringDialogue} " +
				$"scaleAtZoom2={scaleAtZoom2} scaleAtZoom2DuringStory={scaleAtZoom2DuringStory} scaleAfterRebuild={scaleAfterRebuild}");

			if (failures.Count > 0)
			{
				throw new InvalidOperationException($"Large-map story zoom invariant violated: {string.Join("; ", failures)}.");
			}

			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"LARGE_MAP_ZOOM_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}

	private async Task SettleFramesAsync()
	{
		for (var frame = 0; frame < 3; frame++)
		{
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
		}
	}
}
