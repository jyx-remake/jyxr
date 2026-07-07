namespace Game.Core.Persistence;

public sealed record MiniGameStateRecord(
    IReadOnlyDictionary<string, int> PracticePoints,
    IReadOnlyList<string> ClaimedUniqueRewardIds);
