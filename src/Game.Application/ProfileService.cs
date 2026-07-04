using Game.Core.Model;
using Game.Core.Persistence;

namespace Game.Application;

public sealed class ProfileService
{
    private readonly GameSession _session;
    private readonly IDiagnosticLogger _logger;

    public ProfileService(GameSession session, IDiagnosticLogger? logger = null)
    {
        _session = session;
        _logger = logger ?? NullDiagnosticLogger.Instance;
    }

    private GameProfile Profile => _session.Profile;

    public GameProfileRecord CreateProfileRecord()
    {
        var record = GameProfileRecord.Create(Profile);
        _logger.Info($"Created game profile record with {record.UnlockedAchievementIds.Count} unlocked achievement(s).");
        return record;
    }

    public void LoadProfile(GameProfileRecord profileRecord)
    {
        ArgumentNullException.ThrowIfNull(profileRecord);

        var profile = profileRecord.Restore();
        _session.ReplaceProfile(profile);
        _session.Events.Publish(new ProfileLoadedEvent());
        _logger.Info($"Loaded game profile with {profile.UnlockedAchievementIds.Count} unlocked achievement(s).");
    }

    public bool UnlockAchievement(string achievementId)
    {
        if (!Profile.UnlockAchievement(achievementId))
        {
            return false;
        }

        _session.Events.Publish(new AchievementUnlockedEvent(achievementId));
        _session.Events.Publish(new ProfileChangedEvent());
        _logger.Info($"Unlocked achievement: {achievementId}");
        return true;
    }

    public void AddSkillMaxLevelBonus(string skillId, int levels)
    {
        Profile.AddSkillMaxLevelBonus(skillId, levels);
        _logger.Info($"Added {levels} skill max level bonus to '{skillId}'.");
    }

    public bool TryAddSkillMaxLevelBonusOnce(string skillId, int levels, string? onceKey)
    {
        var applied = Profile.TryAddSkillMaxLevelBonusOnce(skillId, levels, onceKey);
        if (applied)
        {
            _logger.Info($"Added {levels} skill max level bonus to '{skillId}'.");
        }
        else
        {
            _logger.Info($"Skipped consumed skill max level bonus key '{onceKey}' for '{skillId}'.");
        }

        return applied;
    }

    public void AddDeaths(int count = 1)
    {
        Profile.AddDeaths(count);
        _session.Events.Publish(new ProfileChangedEvent());
        _logger.Info($"Added {count} death(s) to game profile.");
    }

    public void AddKills(int count = 1)
    {
        Profile.AddKills(count);
        _session.Events.Publish(new ProfileChangedEvent());
        _logger.Info($"Added {count} kill(s) to game profile.");
    }

    public void AdvanceZhenlongqijuLevel()
    {
        Profile.AdvanceZhenlongqijuLevel();
        _session.Events.Publish(new ProfileChangedEvent());
        _logger.Info($"Advanced zhenlongqiju level to {Profile.ZhenlongqijuLevel}.");
    }

    public void ChangeYuanbao(int delta)
    {
        Profile.ChangeYuanbao(delta);
        _session.Events.Publish(new ProfileChangedEvent());
        _logger.Info($"Changed yuanbao by {delta}. Current yuanbao: {Profile.Yuanbao}.");
    }

    public void AddYuanbao(int amount)
    {
        Profile.AddYuanbao(amount);
        _session.Events.Publish(new ProfileChangedEvent());
        _logger.Info($"Added {amount} yuanbao. Current yuanbao: {Profile.Yuanbao}.");
    }

    public void SpendYuanbao(int amount)
    {
        Profile.SpendYuanbao(amount);
        _session.Events.Publish(new ProfileChangedEvent());
        _logger.Info($"Spent {amount} yuanbao. Current yuanbao: {Profile.Yuanbao}.");
    }

    public bool CanSpendYuanbao(int amount) => Profile.CanSpendYuanbao(amount);
}
