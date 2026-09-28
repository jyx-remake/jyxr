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
        profile.AddSaves(4);
        profile.RecordCompletion(3);
        profile.RecordRoundReached(5);
        profile.SetZhenlongqijuLevel(7);
        profile.SetYuanbao(11);
        profile.SetTotalPlayTimeSeconds(3723);
        profile.AddSkillMaxLevelBonus("dragon_palm", 3);
        profile.AddSkillMaxLevelBonus("dragon_palm", 2);
        Assert.True(profile.TryAddSkillMaxLevelBonusOnce("yijinjing", 1, "reward.yijinjing.mastery"));
        Assert.False(profile.TryAddSkillMaxLevelBonusOnce("yijinjing", 1, "reward.yijinjing.mastery"));

        var record = GameProfileRecord.Create(profile);
        var json = JsonSerializer.Serialize(record, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<GameProfileRecord>(json, GameJson.Default);

        Assert.NotNull(roundTripped);
        Assert.Equal(GameProfileRecord.CurrentVersion, record.Version);
        Assert.Contains("\"UnlockedAchievementIds\"", json, StringComparison.Ordinal);
        Assert.Contains("\"DeathCount\":2", json, StringComparison.Ordinal);
        Assert.Contains("\"KillCount\":5", json, StringComparison.Ordinal);
        Assert.Contains("\"SaveCount\":4", json, StringComparison.Ordinal);
        Assert.Contains("\"CompletionCount\":1", json, StringComparison.Ordinal);
        Assert.Contains("\"HighestRound\":5", json, StringComparison.Ordinal);
        Assert.Contains("\"ZhenlongqijuLevel\":7", json, StringComparison.Ordinal);
        Assert.Contains("\"Yuanbao\":11", json, StringComparison.Ordinal);
        Assert.Contains("\"TotalPlayTimeSeconds\":3723", json, StringComparison.Ordinal);
        Assert.Contains("\"dragon_palm\":5", json, StringComparison.Ordinal);
        Assert.Contains("reward.yijinjing.mastery", json, StringComparison.Ordinal);

        var restored = roundTripped!.Restore();
        Assert.True(restored.IsAchievementUnlocked("first_blood"));
        Assert.True(restored.IsAchievementUnlocked("jianghu_veteran"));
        Assert.Equal(2, restored.DeathCount);
        Assert.Equal(5, restored.KillCount);
        Assert.Equal(4, restored.SaveCount);
        Assert.Equal(1, restored.CompletionCount);
        Assert.Equal(5, restored.HighestRound);
        Assert.Equal(7, restored.ZhenlongqijuLevel);
        Assert.Equal(11, restored.Yuanbao);
        Assert.Equal(3723, restored.TotalPlayTimeSeconds);
        Assert.Equal(5, restored.GetSkillMaxLevelBonus("dragon_palm"));
        Assert.Equal(1, restored.GetSkillMaxLevelBonus("yijinjing"));
        Assert.Contains("reward.yijinjing.mastery", restored.ConsumedSkillMaxLevelKeys);
    }

    [Fact]
    public void GameProfileRecord_RestoresMissingYuanbaoAsZero()
    {
        const string json = """
        {
            "Version": 4,
            "UnlockedAchievementIds": [],
            "DeathCount": 0,
            "KillCount": 0,
            "ZhenlongqijuLevel": 0
        }
        """;

        var record = JsonSerializer.Deserialize<GameProfileRecord>(json, GameJson.Default);

        Assert.NotNull(record);
        Assert.Equal(0, record!.Restore().Yuanbao);
        Assert.Equal(0, record.Restore().CompletionCount);
        Assert.Equal(0, record.Restore().HighestRound);
        Assert.Equal(0, record.Restore().SaveCount);
    }

    [Fact]
    public void GameProfileRecord_CurrentVersionWithoutPlayTimeRestoresZero()
    {
        const string json = """
        {
            "Version": 6,
            "UnlockedAchievementIds": [],
            "DeathCount": 0,
            "KillCount": 0
        }
        """;

        var record = JsonSerializer.Deserialize<GameProfileRecord>(json, GameJson.Default);

        Assert.NotNull(record);
        Assert.Equal(6, GameProfileRecord.CurrentVersion);
        Assert.Equal(0, record!.Restore().TotalPlayTimeSeconds);
    }

    [Fact]
    public void GameProfileRecord_RestoresVersionFiveWithoutCompletionStatsAsZero()
    {
        const string json = """
        {
            "Version": 5,
            "UnlockedAchievementIds": [],
            "DeathCount": 2,
            "KillCount": 3,
            "ZhenlongqijuLevel": 0,
            "Yuanbao": 0,
            "SkillMaxLevelBonuses": {},
            "ConsumedSkillMaxLevelKeys": []
        }
        """;

        var record = JsonSerializer.Deserialize<GameProfileRecord>(json, GameJson.Default);

        Assert.NotNull(record);
        var restored = record!.Restore();
        Assert.Equal(0, restored.CompletionCount);
        Assert.Equal(0, restored.HighestRound);
        Assert.Equal(0, restored.SaveCount);
    }

    [Fact]
    public void GameProfileRecord_RestoresVersionSixWithoutSaveCountAsZero()
    {
        const string json = """
        {
            "Version": 6,
            "UnlockedAchievementIds": [],
            "DeathCount": 2,
            "KillCount": 3,
            "CompletionCount": 4,
            "HighestRound": 5
        }
        """;

        var record = JsonSerializer.Deserialize<GameProfileRecord>(json, GameJson.Default);

        Assert.NotNull(record);
        Assert.Equal(0, record!.Restore().SaveCount);
        Assert.Equal(6, GameProfileRecord.CurrentVersion);
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
        session.ProfileService.AddSaves(4);

        Assert.True(firstUnlock);
        Assert.False(secondUnlock);
        Assert.True(session.Profile.IsAchievementUnlocked("first_blood"));
        Assert.Equal(2, session.Profile.DeathCount);
        Assert.Equal(3, session.Profile.KillCount);
        Assert.Equal(4, session.Profile.SaveCount);
        Assert.Single(publishedEvents.OfType<AchievementUnlockedEvent>());
        Assert.Equal(4, publishedEvents.OfType<ProfileChangedEvent>().Count());
    }

    [Fact]
    public void ProfileService_ChangesYuanbao_PublishesOnlyProfileChangedEvent()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var publishedEvents = CollectPublishedEvents(session);

        session.ProfileService.AddYuanbao(7);
        session.ProfileService.SpendYuanbao(2);
        session.ProfileService.ChangeYuanbao(0);

        Assert.Equal(5, session.Profile.Yuanbao);
        Assert.True(session.ProfileService.CanSpendYuanbao(5));
        Assert.False(session.ProfileService.CanSpendYuanbao(6));
        Assert.Equal(3, publishedEvents.OfType<ProfileChangedEvent>().Count());
        Assert.Empty(publishedEvents.OfType<CurrencyChangedEvent>());
    }

    [Fact]
    public void ProfileService_RecordsCompletionsAndOnlyPublishesForHigherReachedRound()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var publishedEvents = CollectPublishedEvents(session);

        session.ProfileService.RecordRoundReached(2);
        session.ProfileService.RecordRoundReached(1);
        session.ProfileService.RecordCompletion(2);
        session.ProfileService.RecordRoundReached(4);
        session.ProfileService.RecordRoundReached(4);

        Assert.Equal(1, session.Profile.CompletionCount);
        Assert.Equal(4, session.Profile.HighestRound);
        Assert.Equal(3, publishedEvents.OfType<ProfileChangedEvent>().Count());
    }

    [Fact]
    public void GameProfile_CompletionCountUsesCheckedArithmetic()
    {
        var profile = new GameProfile();
        profile.SetCompletionStats(int.MaxValue, 1);

        Assert.Throws<OverflowException>(() => profile.RecordCompletion(1));
    }

    [Fact]
    public void GameProfile_KillAndSaveCountsValidateCountAndUseCheckedArithmetic()
    {
        var killProfile = new GameProfile();
        var saveProfile = new GameProfile();

        Assert.Throws<ArgumentOutOfRangeException>(() => killProfile.AddKills(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => killProfile.AddKills(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => saveProfile.AddSaves(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => saveProfile.AddSaves(-1));

        killProfile.SetLifetimeStats(0, int.MaxValue, 0);
        saveProfile.SetLifetimeStats(0, 0, int.MaxValue);
        Assert.Throws<OverflowException>(() => killProfile.AddKills());
        Assert.Throws<OverflowException>(() => saveProfile.AddSaves());
    }

    [Fact]
    public void ProfileService_RejectsInvalidYuanbaoChanges()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());

        Assert.Throws<ArgumentOutOfRangeException>(() => session.ProfileService.AddYuanbao(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.ProfileService.SpendYuanbao(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.ProfileService.ChangeYuanbao(int.MinValue));
        Assert.Throws<InvalidOperationException>(() => session.ProfileService.SpendYuanbao(1));
        Assert.Throws<InvalidOperationException>(() => session.ProfileService.ChangeYuanbao(-1));
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
        sourceProfile.AddSaves(8);
        sourceProfile.RecordCompletion(3);
        sourceProfile.RecordRoundReached(6);
        sourceProfile.SetZhenlongqijuLevel(3);
        var record = GameProfileRecord.Create(sourceProfile);

        session.ProfileService.LoadProfile(record);

        Assert.True(session.Profile.IsAchievementUnlocked("jianghu_veteran"));
        Assert.Equal(4, session.Profile.DeathCount);
        Assert.Equal(9, session.Profile.KillCount);
        Assert.Equal(8, session.Profile.SaveCount);
        Assert.Equal(1, session.Profile.CompletionCount);
        Assert.Equal(6, session.Profile.HighestRound);
        Assert.Equal(3, session.Profile.ZhenlongqijuLevel);
        Assert.Single(publishedEvents.OfType<ProfileLoadedEvent>());
        Assert.Empty(publishedEvents.OfType<ProfileChangedEvent>());
    }

    [Fact]
    public void ProfileService_AddSkillMaxLevelBonus_PublishesProfileChangedEvent()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var publishedEvents = CollectPublishedEvents(session);

        session.ProfileService.AddSkillMaxLevelBonus("dragon_palm", 3);
        session.ProfileService.AddSkillMaxLevelBonus("dragon_palm", 2);

        Assert.Equal(5, session.Profile.GetSkillMaxLevelBonus("dragon_palm"));
        Assert.Equal(2, publishedEvents.OfType<ProfileChangedEvent>().Count());
    }

    [Fact]
    public void ProfileService_TryAddSkillMaxLevelBonusOnce_PublishesOnlyWhenApplied()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var publishedEvents = CollectPublishedEvents(session);

        var first = session.ProfileService.TryAddSkillMaxLevelBonusOnce(
            "dragon_palm",
            3,
            "reward.dragon_palm.mastery");
        var second = session.ProfileService.TryAddSkillMaxLevelBonusOnce(
            "dragon_palm",
            3,
            "reward.dragon_palm.mastery");
        var repeated = session.ProfileService.TryAddSkillMaxLevelBonusOnce(
            "dragon_palm",
            2,
            null);

        Assert.True(first);
        Assert.False(second);
        Assert.True(repeated);
        Assert.Equal(5, session.Profile.GetSkillMaxLevelBonus("dragon_palm"));
        Assert.Contains("reward.dragon_palm.mastery", session.Profile.ConsumedSkillMaxLevelKeys);
        Assert.Equal(2, publishedEvents.OfType<ProfileChangedEvent>().Count());
    }

    private static List<object> CollectPublishedEvents(GameSession session)
    {
        var publishedEvents = new List<object>();
        session.Events.SubscribeAll(publishedEvents.Add);
        return publishedEvents;
    }
}
