using Godot;

namespace Game.Godot.Map;

/// <summary>
/// Small-map location strip layout. Mirrors the legacy MapRoleUI count
/// tiers: every tier fixes how many nodes go into the first row, nodes keep
/// their natural size and flow left to right, and the whole grid is scaled
/// by the screen width ratio (like the legacy canvas scaler) with a final
/// fit clamp so extreme counts never overflow the strip.
/// </summary>
internal static class SmallMapLocationLayout
{
	/// <summary>Design width the node art is authored against.</summary>
	public const float DesignWidth = 1920f;

	public static int ResolveColumns(int count) => count switch
	{
		<= 0 => 1,
		<= 7 => count,
		<= 9 => count - 4,
		<= 11 => 5,
		<= 13 => 6,
		<= 14 => 7,
		<= 16 => 8,
		<= 19 => 7,
		_ => 8,
	};

	public static int ResolveRows(int count, int columns)
	{
		columns = Math.Max(1, columns);
		return Math.Max(1, (Math.Max(0, count) + columns - 1) / columns);
	}

	public static Vector2 ResolveGridSize(int columns, int rows, Vector2 cellSize, Vector2 separation) => new(
		columns * cellSize.X + (columns - 1) * separation.X,
		rows * cellSize.Y + (rows - 1) * separation.Y);

	public static float ResolveScale(Vector2 availableSize, Vector2 gridSize)
	{
		if (gridSize.X <= 0f || gridSize.Y <= 0f)
		{
			return 1f;
		}

		var fit = Math.Min(availableSize.X / gridSize.X, availableSize.Y / gridSize.Y);
		return Math.Min(1f, fit);
	}
}
