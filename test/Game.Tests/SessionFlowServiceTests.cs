using Game.Application;
using Game.Core.Model;

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
}
