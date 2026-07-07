using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class MiniGameState
{
    private readonly Dictionary<string, int> _practicePoints = new(StringComparer.Ordinal);
    private readonly HashSet<string> _claimedUniqueRewardIds = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> PracticePoints => _practicePoints;

    public IReadOnlyCollection<string> ClaimedUniqueRewardIds => _claimedUniqueRewardIds;

    public static MiniGameState Restore(MiniGameStateRecord? record)
    {
        var state = new MiniGameState();
        if (record is null)
        {
            return state;
        }

        foreach (var (key, points) in record.PracticePoints)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (points != 0)
            {
                state._practicePoints[key] = points;
            }
        }

        foreach (var itemId in record.ClaimedUniqueRewardIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
            state._claimedUniqueRewardIds.Add(itemId);
        }

        return state;
    }

    public int GetPracticePoints(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _practicePoints.GetValueOrDefault(key);
    }

    public void SetPracticePoints(string key, int points)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (points == 0)
        {
            _practicePoints.Remove(key);
            return;
        }

        _practicePoints[key] = points;
    }

    public bool IsUniqueRewardClaimed(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        return _claimedUniqueRewardIds.Contains(itemId);
    }

    public void MarkUniqueRewardClaimed(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        _claimedUniqueRewardIds.Add(itemId);
    }

    public MiniGameStateRecord ToRecord() =>
        new(
            _practicePoints
                .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
                .ToDictionary(static entry => entry.Key, static entry => entry.Value, StringComparer.Ordinal),
            _claimedUniqueRewardIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray());
}
