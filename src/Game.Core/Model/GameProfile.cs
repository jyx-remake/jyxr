namespace Game.Core.Model;

public sealed class GameProfile
{
    private readonly HashSet<string> _unlockedAchievementIds = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> UnlockedAchievementIds => _unlockedAchievementIds;

    public int DeathCount { get; private set; }

    public int KillCount { get; private set; }

    public int ZhenlongqijuLevel { get; private set; }

    public bool IsAchievementUnlocked(string achievementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementId);
        return _unlockedAchievementIds.Contains(achievementId);
    }

    public bool UnlockAchievement(string achievementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementId);
        return _unlockedAchievementIds.Add(achievementId);
    }

    public void AddDeaths(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        DeathCount += count;
    }

    public void AddKills(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        KillCount += count;
    }

    public void SetZhenlongqijuLevel(int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ZhenlongqijuLevel = level;
    }

    public void AdvanceZhenlongqijuLevel() => ZhenlongqijuLevel++;

    public void SetUnlockedAchievementIds(IEnumerable<string> achievementIds)
    {
        ArgumentNullException.ThrowIfNull(achievementIds);

        _unlockedAchievementIds.Clear();
        foreach (var achievementId in achievementIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(achievementId);
            _unlockedAchievementIds.Add(achievementId);
        }
    }

    public void SetLifetimeStats(int deathCount, int killCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(deathCount);
        ArgumentOutOfRangeException.ThrowIfNegative(killCount);

        DeathCount = deathCount;
        KillCount = killCount;
    }
}
