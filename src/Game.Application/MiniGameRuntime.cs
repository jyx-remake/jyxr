namespace Game.Application;

public interface IMiniGameRuntimeHost
{
    ValueTask<int> RunLightnessTrainingAsync(CancellationToken cancellationToken);

    ValueTask<(int Score, IReadOnlyDictionary<string, int> ItemCounts)> RunStrengthTrainingAsync(
        IReadOnlyList<string> itemIds,
        CancellationToken cancellationToken);
}
