namespace Game.Core.Persistence;

public sealed record SpecialBattleStateRecord(
    IReadOnlyList<string> TrialCompletedCharacterIds,
    IReadOnlyDictionary<string, int> TowerRewardClaimCounts);
