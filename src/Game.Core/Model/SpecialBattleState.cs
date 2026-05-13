using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class SpecialBattleState
{
    private readonly HashSet<string> _trialCompletedCharacterIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _towerRewardClaimCounts = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> TrialCompletedCharacterIds => _trialCompletedCharacterIds;

    public IReadOnlyDictionary<string, int> TowerRewardClaimCounts => _towerRewardClaimCounts;

    public static SpecialBattleState Restore(SpecialBattleStateRecord? record)
    {
        var state = new SpecialBattleState();
        if (record is null)
        {
            return state;
        }

        foreach (var characterId in record.TrialCompletedCharacterIds)
        {
            state.MarkTrialCompleted(characterId);
        }

        foreach (var (claimKey, count) in record.TowerRewardClaimCounts)
        {
            if (count > 0)
            {
                state._towerRewardClaimCounts[claimKey] = count;
            }
        }

        return state;
    }

    public bool IsTrialCompleted(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        return _trialCompletedCharacterIds.Contains(characterId);
    }

    public bool MarkTrialCompleted(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        return _trialCompletedCharacterIds.Add(characterId);
    }

    public int GetTowerRewardClaimCount(string towerId, string stageId, string rewardId)
    {
        var claimKey = CreateTowerRewardClaimKey(towerId, stageId, rewardId);
        return _towerRewardClaimCounts.GetValueOrDefault(claimKey);
    }

    public void AddTowerRewardClaim(string towerId, string stageId, string rewardId)
    {
        var claimKey = CreateTowerRewardClaimKey(towerId, stageId, rewardId);
        _towerRewardClaimCounts[claimKey] = GetTowerRewardClaimCount(towerId, stageId, rewardId) + 1;
    }

    public SpecialBattleStateRecord ToRecord() =>
        new(
            _trialCompletedCharacterIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray(),
            new Dictionary<string, int>(_towerRewardClaimCounts, StringComparer.Ordinal));

    private static string CreateTowerRewardClaimKey(string towerId, string stageId, string rewardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(towerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rewardId);
        return string.Join('|', towerId, stageId, rewardId);
    }
}
