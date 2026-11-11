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

    public void StartNewGame()
    {
        _session.PlayTimeService.Stop();
        ReplaceState(_newGameStateFactory.Create(Config.InitialPartyCharacterIds));
        _session.PlayTimeService.ResetInterval();
        _session.ProfileService.RecordRoundReached(State.Adventure.Round);
    }

    public void RestartCurrentRound()
    {
        StartRound(State.Adventure.Round);
    }

    public void StartNextRound()
    {
        StartRound(checked(State.Adventure.Round + 1));
    }

    public async ValueTask<StoryCommandResult> EndAsync(
        StoryOutcome outcome,
        IRuntimeHost host,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = State;
        if (_session.Execution.IsTerminal && outcome != StoryOutcome.MainMenu)
        {
            return StoryCommandResult.End(_session.Execution.GetOutcome(state));
        }

        _session.Execution.End(outcome);
        switch (outcome)
        {
            case StoryOutcome.Defeated:
                _session.ProfileService.AddDeaths();
                await host.GameOverAsync(cancellationToken);
                break;
            case StoryOutcome.GameCompleted:
                _session.ProfileService.RecordCompletion(state.Adventure.Round);
                await host.GameCompleteAsync(cancellationToken);
                break;
            case StoryOutcome.MainMenu:
                await host.MainMenuAsync(cancellationToken);
                break;
        }
        return StoryCommandResult.End(_session.Execution.GetOutcome(state));
    }

    private void StartRound(int round)
    {
        _session.PlayTimeService.Checkpoint();
        var lastTrialCount = State.SpecialBattle.TrialCompletedCharacterIds.Count;
        var carriedChest = State.Chest.Clone(ContentRepository);
        var nextState = _newGameStateFactory.Create(
            Config.InitialPartyCharacterIds,
            round,
            carriedChest);
        nextState.Currency.AddSilver(NextRoundInitialSilver);
        nextState.SetPlayTimeSeconds(State.PlayTimeSeconds);
        nextState.Story.SetVariable("last_trial_count", ExpressionValue.FromNumber(lastTrialCount));

        ReplaceState(nextState);
        _session.ProfileService.RecordRoundReached(round);
    }

    private void ReplaceState(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _session.ReplaceState(state);
        _session.Events.Publish(new SaveLoadedEvent());
    }
}
