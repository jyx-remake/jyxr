using Game.Core.Abstractions;

namespace Game.Core;

public static class Probability
{
    public static bool RollChance(IRandomService random, double probability)
    {
        ArgumentNullException.ThrowIfNull(random);
        return probability switch
        {
            <= 0d => false,
            >= 1d => true,
            _ => random.NextDouble() < probability,
        };
    }

    public static bool RollChance(double probability) =>
        probability switch
        {
            <= 0d => false,
            >= 1d => true,
            _ => Random.Shared.NextDouble() < probability,
        };

    public static bool RollPercentage(IRandomService random, int percentage) =>
        RollChance(random, percentage / 100d);

    public static bool RollPercentage(int percentage) =>
        RollChance(percentage / 100d);
}
