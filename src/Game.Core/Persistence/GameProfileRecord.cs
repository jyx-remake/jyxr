using Game.Core.Model;

namespace Game.Core.Persistence;

public sealed record GameProfileRecord(
    int Version,
    IReadOnlyList<string> UnlockedAchievementIds,
    int DeathCount,
    int KillCount,
    int ZhenlongqijuLevel = 0)
{
    public const int CurrentVersion = 2;

    public static GameProfileRecord Create(GameProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new GameProfileRecord(
            CurrentVersion,
            profile.UnlockedAchievementIds.OrderBy(static id => id, StringComparer.Ordinal).ToList(),
            profile.DeathCount,
            profile.KillCount,
            profile.ZhenlongqijuLevel);
    }

    public GameProfile Restore()
    {
        var profile = new GameProfile();
        profile.SetUnlockedAchievementIds(UnlockedAchievementIds);
        profile.SetLifetimeStats(DeathCount, KillCount);
        profile.SetZhenlongqijuLevel(ZhenlongqijuLevel);
        return profile;
    }
}
