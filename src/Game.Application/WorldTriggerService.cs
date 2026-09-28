using Game.Core;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Application;

public sealed class WorldTriggerService
{
    private readonly GameSession _session;
    private readonly GameConditionExpressionService _conditions;
    private GameState? _pendingState;
    private GameState? _executingState;

    public WorldTriggerService(GameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _conditions = new GameConditionExpressionService(session);
    }

    private GameState State => _session.State;

    public bool HasPendingCheck => ReferenceEquals(_pendingState, State);

    public void RequestCheck()
    {
        // Map commands inside a global event must not schedule that event again.
        if (!ReferenceEquals(_executingState, State))
        {
            _pendingState = State;
        }
    }

    public async Task<bool> ExecutePendingAsync(CancellationToken cancellationToken = default)
    {
        if (!HasPendingCheck || _executingState is not null || State.WorldTriggers.IsBlocked)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        _pendingState = null;
        var trigger = _session.ContentRepository.GetWorldTriggers().FirstOrDefault(candidate =>
            !IsCompleted(candidate) && _conditions.Evaluate(candidate.When));
        if (trigger is null)
        {
            return false;
        }

        var executionState = State;
        _executingState = executionState;
        try
        {
            await _session.StoryService.CommandDispatcher.ExecuteCallAsync(trigger.Action, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(executionState, State))
            {
                return false;
            }

            if (trigger.RepeatMode == RepeatMode.Once)
            {
                executionState.WorldTriggers.MarkCompleted(trigger.Id);
            }

            _session.Events.Publish(new AutoSaveRequestedEvent($"world trigger '{trigger.Id}' completed"));
            return true;
        }
        finally
        {
            _executingState = null;
        }
    }

    public void Block() => State.WorldTriggers.Block();

    public void Unblock() => State.WorldTriggers.Unblock();

    private bool IsCompleted(WorldTriggerDefinition trigger) =>
        trigger.RepeatMode == RepeatMode.Once && State.WorldTriggers.IsCompleted(trigger.Id);
}
