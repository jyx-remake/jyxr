using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Application;

public sealed class SessionFlowService
{
    private const int NextRoundInitialSilver = 100;

    private readonly GameSession _session;
    private readonly NewGameStateFactory _newGameStateFactory;

    public SessionFlowService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _newGameStateFactory = new NewGameStateFactory(
            session.ContentRepository,
            session.Config,
            profileProvider: () => session.Profile);
    }

    private GameConfig Config => _session.Config;
    private IContentRepository ContentRepository => _session.ContentRepository;
    private GameState State => _session.State;

    public void StartNewGame() =>
        ReplaceState(_newGameStateFactory.Create(Config.InitialPartyCharacterIds));

    public void StartNextRound()
    {
        var nextRound = checked(State.Adventure.Round + 1);
        var lastTrialCount = State.SpecialBattle.TrialCompletedCharacterIds.Count;
        var carriedChest = State.Chest.Clone(ContentRepository);
        var nextState = _newGameStateFactory.Create(
            Config.InitialPartyCharacterIds,
            nextRound,
            carriedChest);
        nextState.Currency.AddSilver(NextRoundInitialSilver);
        nextState.Story.SetVariable("last_trial_count", ExprValue.FromNumber(lastTrialCount));

        ReplaceState(nextState);
    }

    private void ReplaceState(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _session.ReplaceState(state);
        _session.Events.Publish(new SaveLoadedEvent());
    }
}
