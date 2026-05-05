namespace Game.Core.Model;

public readonly record struct MapPosition(int X, int Y)
{
    public static MapPosition Zero { get; } = new(0, 0);

    public double DistanceTo(MapPosition other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
