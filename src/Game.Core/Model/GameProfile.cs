namespace Game.Core.Model;

public sealed class GameProfile
{
    private readonly HashSet<string> _unlockedAchievementIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _skillMaxLevelBonuses = new(StringComparer.Ordinal);
    private readonly HashSet<string> _consumedSkillMaxLevelKeys = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> UnlockedAchievementIds => _unlockedAchievementIds;
    public IReadOnlyDictionary<string, int> SkillMaxLevelBonuses => _skillMaxLevelBonuses;
    public IReadOnlyCollection<string> ConsumedSkillMaxLevelKeys => _consumedSkillMaxLevelKeys;

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

    public int GetSkillMaxLevelBonus(string skillId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
        return _skillMaxLevelBonuses.GetValueOrDefault(skillId);
    }

    public void AddSkillMaxLevelBonus(string skillId, int levels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);
        _skillMaxLevelBonuses[skillId] = checked(GetSkillMaxLevelBonus(skillId) + levels);
    }

    public bool TryAddSkillMaxLevelBonusOnce(string skillId, int levels, string? onceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);
        if (string.IsNullOrWhiteSpace(onceKey))
        {
            AddSkillMaxLevelBonus(skillId, levels);
            return true;
        }

        if (_consumedSkillMaxLevelKeys.Contains(onceKey))
        {
            return false;
        }

        AddSkillMaxLevelBonus(skillId, levels);
        _consumedSkillMaxLevelKeys.Add(onceKey);
        return true;
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

    public void SetSkillMaxLevelBonuses(IReadOnlyDictionary<string, int> bonuses)
    {
        ArgumentNullException.ThrowIfNull(bonuses);

        _skillMaxLevelBonuses.Clear();
        foreach (var (skillId, bonus) in bonuses)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
            ArgumentOutOfRangeException.ThrowIfNegative(bonus);
            if (bonus > 0)
            {
                _skillMaxLevelBonuses[skillId] = bonus;
            }
        }
    }

    public void SetConsumedSkillMaxLevelKeys(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        _consumedSkillMaxLevelKeys.Clear();
        foreach (var key in keys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            _consumedSkillMaxLevelKeys.Add(key);
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
