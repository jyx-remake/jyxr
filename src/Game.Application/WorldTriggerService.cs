using Game.Core;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Application;

public sealed class WorldTriggerService
{
    private readonly GameSession _session;
    private readonly GameConditionExpressionService _conditions;
    private GameState? _pendingState;
    private GameState? _executingState;
    private readonly HashSet<string> _executedInCheck = new(StringComparer.Ordinal);

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
            if (!HasPendingCheck) _executedInCheck.Clear();
            _pendingState = State;
        }
    }

    public async Task<bool> ExecutePendingAsync(CancellationToken cancellationToken = default)
    {
        if (!HasPendingCheck || _executingState is not null || State.WorldTriggers.IsBlocked || !_session.Execution.CanSave)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        _pendingState = null;
        var trigger = _session.ContentRepository.GetWorldTriggers().FirstOrDefault(candidate =>
            !_executedInCheck.Contains(candidate.Id) && !IsCompleted(candidate) && _conditions.Evaluate(candidate.When));
        if (trigger is null)
        {
            return false;
        }

        var executionState = State;
        using var execution = _session.Execution.Begin($"world trigger '{trigger.Id}'", cancellationToken);
        _executingState = executionState;
        try
        {
            await _session.StoryService.CommandDispatcher.ExecuteCallAsync(trigger.Action, execution.Token);
            cancellationToken.ThrowIfCancellationRequested();
            if (execution.Outcome != StoryOutcome.Completed)
            {
                return false;
            }

            if (trigger.RepeatMode == RepeatMode.Once)
            {
                executionState.WorldTriggers.MarkCompleted(trigger.Id);
            }

            _executedInCheck.Add(trigger.Id);
            execution.Complete();
            _session.MapService.ReturnToMap(executionState, $"world trigger '{trigger.Id}' completed");
            _pendingState = executionState;
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
