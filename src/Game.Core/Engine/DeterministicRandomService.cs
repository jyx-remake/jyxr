using Game.Core.Abstractions;

namespace Game.Core.Engine;

public sealed class DeterministicRandomService : IRandomService
{
    private readonly Random _random;

    public DeterministicRandomService(int seed)
    {
        _random = new Random(seed);
    }

    public double NextDouble() => _random.NextDouble();

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
