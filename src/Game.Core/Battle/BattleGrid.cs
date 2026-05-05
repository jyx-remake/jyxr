using Game.Core.Model;

namespace Game.Core.Battle;

public sealed class BattleGrid
{
    private readonly HashSet<GridPosition> _blocked;

    public BattleGrid(int width, int height, IEnumerable<GridPosition>? blocked = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        Width = width;
        Height = height;
        _blocked = blocked?.ToHashSet() ?? [];
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlySet<GridPosition> Blocked => _blocked;

    public bool Contains(GridPosition position) =>
        position.X >= 0 &&
        position.Y >= 0 &&
        position.X < Width &&
        position.Y < Height;

    public bool IsBlocked(GridPosition position) => _blocked.Contains(position);

    public bool IsWalkable(GridPosition position) => Contains(position) && !IsBlocked(position);
}
