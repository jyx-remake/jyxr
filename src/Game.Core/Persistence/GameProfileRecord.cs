using Game.Core.Model;

namespace Game.Core.Persistence;

public sealed record GameProfileRecord(
    int Version,
    IReadOnlyList<string> UnlockedAchievementIds,
    int DeathCount,
    int KillCount)
{
    public const int CurrentVersion = 1;

    public static GameProfileRecord Create(GameProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new GameProfileRecord(
            CurrentVersion,
            profile.UnlockedAchievementIds.OrderBy(static id => id, StringComparer.Ordinal).ToList(),
            profile.DeathCount,
            profile.KillCount);
    }

    public GameProfile Restore()
    {
        var profile = new GameProfile();
        profile.SetUnlockedAchievementIds(UnlockedAchievementIds);
        profile.SetLifetimeStats(DeathCount, KillCount);
        return profile;
    }
}
