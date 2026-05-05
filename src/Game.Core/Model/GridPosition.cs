namespace Game.Core.Model;

public readonly record struct GridPosition(int X, int Y)
{
    public int ManhattanDistanceTo(GridPosition other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    public IEnumerable<GridPosition> GetOrthogonalNeighbors()
    {
        yield return new GridPosition(X + 1, Y);
        yield return new GridPosition(X - 1, Y);
        yield return new GridPosition(X, Y + 1);
        yield return new GridPosition(X, Y - 1);
    }
}
