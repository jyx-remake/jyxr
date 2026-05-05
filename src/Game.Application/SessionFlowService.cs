using Game.Core.Abstractions;
using Game.Core.Model;

namespace Game.Application;

public sealed class SessionFlowService
{
    private readonly GameSession _session;
    private readonly NewGameStateFactory _newGameStateFactory;

    public SessionFlowService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _newGameStateFactory = new NewGameStateFactory(session.ContentRepository);
    }

    private GameConfig Config => _session.Config;
    private IContentRepository ContentRepository => _session.ContentRepository;
    private GameState State => _session.State;

    public void StartNewGame() =>
        ReplaceState(_newGameStateFactory.Create(Config.InitialPartyCharacterIds));

    public void StartNextRound()
    {
        var nextRound = checked(State.Adventure.Round + 1);
        var carriedGold = State.Currency.Gold;
        var carriedChest = State.Chest.Clone(ContentRepository);

        ReplaceState(_newGameStateFactory.Create(
            Config.InitialPartyCharacterIds,
            nextRound,
            carriedGold,
            carriedChest));
    }

    private void ReplaceState(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _session.ReplaceState(state);
        _session.Events.Publish(new SaveLoadedEvent());
    }
}
