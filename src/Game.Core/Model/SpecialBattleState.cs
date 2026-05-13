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

        foreach (var (contentId, count) in record.TowerRewardClaimCounts)
        {
            if (count > 0)
            {
                state._towerRewardClaimCounts[contentId] = count;
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

    public int GetTowerRewardClaimCount(string contentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        return _towerRewardClaimCounts.GetValueOrDefault(contentId);
    }

    public void AddTowerRewardClaim(string contentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        _towerRewardClaimCounts[contentId] = GetTowerRewardClaimCount(contentId) + 1;
    }

    public SpecialBattleStateRecord ToRecord() =>
        new(
            _trialCompletedCharacterIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray(),
            new Dictionary<string, int>(_towerRewardClaimCounts, StringComparer.Ordinal));
}
