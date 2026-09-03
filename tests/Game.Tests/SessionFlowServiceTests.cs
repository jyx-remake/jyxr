using Game.Application;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Tests;

public sealed class SessionFlowServiceTests
{
    [Fact]
    public void StartNewGame_RecordsFirstRoundAsReached()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(characters: [heroDefinition]),
            config: new GameConfig { InitialPartyCharacterIds = ["hero"] });

        session.SessionFlowService.StartNewGame();

        Assert.Equal(1, session.Profile.HighestRound);
        Assert.Equal(0, session.Profile.CompletionCount);
    }

    [Fact]
    public void StartNextRound_ResetsNoRegret()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var repository = TestContentFactory.CreateRepository(characters: [heroDefinition]);
        var state = new GameState();
        state.Adventure.SetNoRegret(true);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                InitialPartyCharacterIds = ["hero"],
            });

        session.SessionFlowService.StartNextRound();

        Assert.False(session.State.Adventure.NoRegret);
    }

    [Fact]
    public void StartNextRound_ResetsSilverToFixedInitialAmount()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var repository = TestContentFactory.CreateRepository(characters: [heroDefinition]);
        var state = new GameState();
        state.Adventure.SetRound(2);
        state.Currency.AddSilver(999);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                InitialPartyCharacterIds = ["hero"],
            });

        session.SessionFlowService.StartNextRound();

        Assert.Equal(3, session.State.Adventure.Round);
        Assert.Equal(100, session.State.Currency.Silver);
        Assert.Equal(3, session.Profile.HighestRound);
        Assert.Equal(0, session.Profile.CompletionCount);
    }

    [Fact]
    public void StartNextRound_WritesLastTrialCountStoryVariable()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var repository = TestContentFactory.CreateRepository(characters: [heroDefinition]);
        var state = new GameState();
        state.SpecialBattle.MarkTrialCompleted("hero");
        state.SpecialBattle.MarkTrialCompleted("ally");
        state.SpecialBattle.MarkTrialCompleted("guest");
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                InitialPartyCharacterIds = ["hero"],
            });

        session.SessionFlowService.StartNextRound();

        Assert.True(session.State.Story.TryGetVariable("last_trial_count", out var value));
        Assert.Equal(ExpressionValueKind.Number, value.Kind);
        Assert.Equal(3, value.AsInt32("last_trial_count"));
    }

    [Fact]
    public void StartNextRound_CarriesPlayTimeWhileStartNewGameResetsIt()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = new GameState();
        state.SetPlayTimeSeconds(456);
        var profile = new GameProfile();
        profile.SetTotalPlayTimeSeconds(1234);
        var session = new GameSession(
            state,
            TestContentFactory.CreateRepository(characters: [heroDefinition]),
            initialProfile: profile,
            config: new GameConfig { InitialPartyCharacterIds = ["hero"] });

        session.SessionFlowService.StartNextRound();
        Assert.Equal(456, session.State.PlayTimeSeconds);

        session.SessionFlowService.StartNewGame();
        Assert.Equal(0, session.State.PlayTimeSeconds);
        Assert.Equal(1234, session.Profile.TotalPlayTimeSeconds);
    }

    [Fact]
    public void RestartCurrentRound_PreservesRoundAndResetsPlayTime()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = new GameState();
        state.Adventure.SetRound(3);
        state.SetPlayTimeSeconds(456);
        var session = new GameSession(
            state,
            TestContentFactory.CreateRepository(characters: [heroDefinition]),
            config: new GameConfig { InitialPartyCharacterIds = ["hero"] });

        session.SessionFlowService.RestartCurrentRound();

        Assert.Equal(3, session.State.Adventure.Round);
        Assert.Equal(0, session.State.PlayTimeSeconds);
    }
}
