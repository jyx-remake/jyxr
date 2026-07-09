using Game.Application;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Tests;

public sealed class SessionFlowServiceTests
{
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
        Assert.Equal(ExprValueKind.Number, value.Kind);
        Assert.Equal(3, value.AsInt32("last_trial_count"));
    }
}
