namespace Game.Core.Abstractions;

public static class RandomServiceExtensions
{
    public static bool RollChance(this IRandomService random, double probability)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (probability <= 0d)
        {
            return false;
        }

        if (probability >= 1d)
        {
            return true;
        }

        return random.NextDouble() < probability;
    }

    public static bool RollPercentage(this IRandomService random, int percentage) =>
        random.RollChance(percentage / 100d);
}
