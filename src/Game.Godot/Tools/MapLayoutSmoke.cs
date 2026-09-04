using Game.Application.Mods;
using Game.Godot.Map;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless check for the small-map location strip: no scrolling, count
/// tiers match the legacy MapRoleUI layout, and the fitted grid stays
/// inside its area on both a small map (华山) and the largest one (传闻之地).
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/MapLayoutSmoke.cs
/// </summary>
public partial class MapLayoutSmoke : SceneTree
{
	public override void _Initialize() => CallDeferred(nameof(RunSmoke));

	private async void RunSmoke()
	{
		try
		{
			Check(SmallMapLocationLayout.ResolveColumns(0) == 1, "columns(0)");
			Check(SmallMapLocationLayout.ResolveColumns(5) == 5, "columns(5)");
			Check(SmallMapLocationLayout.ResolveColumns(7) == 7, "columns(7)");
			Check(SmallMapLocationLayout.ResolveColumns(8) == 4, "columns(8)");
			Check(SmallMapLocationLayout.ResolveColumns(9) == 5, "columns(9)");
			Check(SmallMapLocationLayout.ResolveColumns(11) == 5, "columns(11)");
			Check(SmallMapLocationLayout.ResolveColumns(12) == 6, "columns(12)");
			Check(SmallMapLocationLayout.ResolveColumns(14) == 7, "columns(14)");
			Check(SmallMapLocationLayout.ResolveColumns(16) == 8, "columns(16)");
			Check(SmallMapLocationLayout.ResolveColumns(17) == 7, "columns(17)");
			Check(SmallMapLocationLayout.ResolveColumns(20) == 8, "columns(20)");
			Check(SmallMapLocationLayout.ResolveRows(13, 7) == 2, "rows(13, 7)");
			var scale = SmallMapLocationLayout.ResolveScale(new Vector2(1648f, 278f), new Vector2(2216f, 816f));
			Check(scale < 1f && scale > 0.3f, $"scale down, got {scale}");
			Check(SmallMapLocationLayout.ResolveScale(new Vector2(1648f, 278f), new Vector2(500f, 256f)) == 1f, "no upscale");

			var projectRoot = ProjectSettings.GlobalizePath("res://");
			var dataRoot = ProjectDataRoot.FromPath(projectRoot);
			var mods = new ModRegistry(dataRoot).DiscoverMods();
			var loadout = new ModLoadoutResolver(mods).Resolve("xmjh", []);

			GameRuntimeBootstrap.Initialize(loadout, this);

			var screenScene = GD.Load<PackedScene>("res://scenes/map/map_screen.tscn")
				?? throw new InvalidOperationException("map_screen.tscn could not be loaded.");
			var screen = screenScene.Instantiate();
			Root.AddChild(screen);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);

			await CheckMapFits(screen, "华山");
			await CheckMapFits(screen, "传闻之地");

			// Synthetic 12-node grid: exercises the real shrink path
			// (scale < 1) through MapScreen.ApplyFit.
			var area = new Control { Size = new Vector2(1728f, 370f) };
			Root.AddChild(area);
			var grid = new GridContainer { Columns = 6 };
			grid.AddThemeConstantOverride("h_separation", 24);
			grid.AddThemeConstantOverride("v_separation", 24);
			area.AddChild(grid);
			for (var index = 0; index < 12; index++)
			{
				grid.AddChild(new Button { CustomMinimumSize = new Vector2(256f, 256f) });
			}

			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			Map.MapScreen.ApplyFit(grid, area.Size, new Vector2(1920f, 1080f));
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			Check(grid.Scale.X < 1f && grid.Scale.X > 0.4f, $"synthetic grid should shrink, got {grid.Scale.X}");
			var syntheticRect = area.GetGlobalRect().Grow(-1f);
			foreach (var child in grid.GetChildren())
			{
				if (child is Control box)
				{
					Check(syntheticRect.Encloses(box.GetGlobalRect()), $"shrunk synthetic box fits, {box.GetGlobalRect()} in {syntheticRect}");
				}
			}

			GD.Print($"MAPLAYOUT_SMOKE synthetic scale={grid.Scale.X}");
			GD.Print("MAPLAYOUT_SMOKE ok");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"MAPLAYOUT_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}

	private async Task CheckMapFits(Node screen, string mapId)
	{
		screen.Call("ShowMap", mapId);
		await ToSignal(this, SceneTree.SignalName.ProcessFrame);
		await ToSignal(this, SceneTree.SignalName.ProcessFrame);
		await ToSignal(this, SceneTree.SignalName.ProcessFrame);

		var grid = screen.GetNode<GridContainer>("%MapEntityList");
		var area = screen.GetNode<Control>("%SmallMapLocationArea");
		Check(grid.GetChildCount() > 0, $"{mapId} should list locations");
		Check(grid.Columns == SmallMapLocationLayout.ResolveColumns(grid.GetChildCount()), $"{mapId} grid columns follow tiers");
		Check(screen.FindChildren("*", "ScrollContainer", true, false).Count == 0, "no scroll container remains");
		Check(grid.Scale.X <= 1f && grid.Scale.X > 0f, $"{mapId} grid scale sane, got {grid.Scale}");
		var areaRect = area.GetGlobalRect().Grow(-1f);
		var liveBoxes = 0;
		Control? firstBox = null;
		foreach (var child in grid.GetChildren())
		{
			if (child is Control box && !child.IsQueuedForDeletion())
			{
				liveBoxes++;
				firstBox ??= box;
				Check(areaRect.Encloses(box.GetGlobalRect()), $"{mapId} box {box.Name} fits, {box.GetGlobalRect()} in {areaRect}");
			}
		}

		Check(liveBoxes > 0, $"{mapId} should have live boxes");
		// Flow is left to right from the strip padding; only the height is
		// centered: the first node sits at the left padding and half a
		// scaled grid above the area middle.
		var expectColumns = grid.Columns;
		var expectRows = (liveBoxes + expectColumns - 1) / expectColumns;
		var expectGrid = new Vector2(
			expectColumns * 256f + (expectColumns - 1) * 24f,
			expectRows * 256f + (expectRows - 1) * 24f);
		var expectFirst = new Vector2(
			areaRect.Position.X + 1f + 40f,
			areaRect.GetCenter().Y - expectGrid.Y * grid.Scale.X / 2f);
		Check(firstBox is not null &&
			(firstBox.GetGlobalRect().Position - expectFirst).Length() < 12f,
			$"{mapId} first box left-flow height-centered, got {firstBox?.GetGlobalRect().Position} expect {expectFirst}");

		GD.Print($"MAPLAYOUT_SMOKE {mapId} locations={grid.GetChildCount()} columns={grid.Columns} scale={grid.Scale.X}");
	}

	private static void Check(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException($"Map layout assertion failed: {message}.");
		}
	}
}
