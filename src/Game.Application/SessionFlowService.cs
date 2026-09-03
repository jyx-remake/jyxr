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
        _session.PlayTimeService.Stop();
        var currentRound = State.Adventure.Round;
        var carriedChest = State.Chest.Clone(ContentRepository);
        ReplaceState(_newGameStateFactory.Create(
            Config.InitialPartyCharacterIds,
            currentRound,
            carriedChest));
        _session.PlayTimeService.ResetInterval();
        _session.ProfileService.RecordRoundReached(currentRound);
    }

    public void StartNextRound()
    {
        _session.PlayTimeService.Checkpoint();
        var nextRound = checked(State.Adventure.Round + 1);
        var lastTrialCount = State.SpecialBattle.TrialCompletedCharacterIds.Count;
        var carriedChest = State.Chest.Clone(ContentRepository);
        var nextState = _newGameStateFactory.Create(
            Config.InitialPartyCharacterIds,
            nextRound,
            carriedChest);
        nextState.Currency.AddSilver(NextRoundInitialSilver);
        nextState.SetPlayTimeSeconds(State.PlayTimeSeconds);
        nextState.Story.SetVariable("last_trial_count", ExpressionValue.FromNumber(lastTrialCount));

        ReplaceState(nextState);
        _session.ProfileService.RecordRoundReached(nextRound);
    }

    private void ReplaceState(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _session.ReplaceState(state);
        _session.InventoryService.RestoreEquipmentGrantedSkills();
        _session.Events.Publish(new SaveLoadedEvent());
    }
}
