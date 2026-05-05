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
}
