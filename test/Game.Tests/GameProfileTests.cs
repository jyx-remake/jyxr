using System.Text.Json;
using Game.Application;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Serialization;

namespace Game.Tests;

public sealed class GameProfileTests
{
    [Fact]
    public void GameProfileRecord_RoundTripsAchievementsAndStats()
    {
        var profile = new GameProfile();
        profile.UnlockAchievement("first_blood");
        profile.UnlockAchievement("jianghu_veteran");
        profile.AddDeaths(2);
        profile.AddKills(5);

        var record = GameProfileRecord.Create(profile);
        var json = JsonSerializer.Serialize(record, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<GameProfileRecord>(json, GameJson.Default);

        Assert.NotNull(roundTripped);
        Assert.Equal(GameProfileRecord.CurrentVersion, record.Version);
        Assert.Contains("\"UnlockedAchievementIds\"", json, StringComparison.Ordinal);
        Assert.Contains("\"DeathCount\":2", json, StringComparison.Ordinal);
        Assert.Contains("\"KillCount\":5", json, StringComparison.Ordinal);

        var restored = roundTripped!.Restore();
        Assert.True(restored.IsAchievementUnlocked("first_blood"));
        Assert.True(restored.IsAchievementUnlocked("jianghu_veteran"));
        Assert.Equal(2, restored.DeathCount);
        Assert.Equal(5, restored.KillCount);
    }

    [Fact]
    public void ProfileService_UnlockAchievementAndAccumulateStats_PublishesEvents()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var publishedEvents = CollectPublishedEvents(session);

        var firstUnlock = session.ProfileService.UnlockAchievement("first_blood");
        var secondUnlock = session.ProfileService.UnlockAchievement("first_blood");
        session.ProfileService.AddDeaths(2);
        session.ProfileService.AddKills(3);

        Assert.True(firstUnlock);
        Assert.False(secondUnlock);
        Assert.True(session.Profile.IsAchievementUnlocked("first_blood"));
        Assert.Equal(2, session.Profile.DeathCount);
        Assert.Equal(3, session.Profile.KillCount);
        Assert.Single(publishedEvents.OfType<AchievementUnlockedEvent>());
        Assert.Equal(3, publishedEvents.OfType<ProfileChangedEvent>().Count());
    }

    [Fact]
    public void ProfileService_LoadProfile_ReplacesProfileAndPublishesLoadedEvent()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var publishedEvents = CollectPublishedEvents(session);
        var sourceProfile = new GameProfile();
        sourceProfile.UnlockAchievement("jianghu_veteran");
        sourceProfile.AddDeaths(4);
        sourceProfile.AddKills(9);
        var record = GameProfileRecord.Create(sourceProfile);

        session.ProfileService.LoadProfile(record);

        Assert.True(session.Profile.IsAchievementUnlocked("jianghu_veteran"));
        Assert.Equal(4, session.Profile.DeathCount);
        Assert.Equal(9, session.Profile.KillCount);
        Assert.Single(publishedEvents.OfType<ProfileLoadedEvent>());
        Assert.Empty(publishedEvents.OfType<ProfileChangedEvent>());
    }

    private static List<object> CollectPublishedEvents(GameSession session)
    {
        var publishedEvents = new List<object>();
        session.Events.SubscribeAll(publishedEvents.Add);
        return publishedEvents;
    }
}
