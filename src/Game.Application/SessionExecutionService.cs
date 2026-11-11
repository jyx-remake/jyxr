using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Story;

namespace Game.Application;

public sealed class SessionExecutionService(GameSession session)
{
    private SessionExecution? _current;
    private SaveGame? _checkpoint;

    public StoryOutcome Outcome { get; private set; } = StoryOutcome.Completed;
    public bool CanSave => _current is null && !IsTerminal;
    public bool IsTerminal => Outcome != StoryOutcome.Completed;

    public SessionExecution Begin(string reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsTerminal)
        {
            throw new InvalidOperationException("当前流程已经结束，请读档或开始新游戏。");
        }

        if (_current is not null)
        {
            return new SessionExecution(this, _current, cancellationToken);
        }

        var checkpoint = session.SaveGameService.CaptureSave();
        if (!string.IsNullOrWhiteSpace(session.State.Location.CurrentMapId))
        {
            PublishCheckpoint(checkpoint, $"before {reason}");
        }

        _current = new SessionExecution(this, session.State, checkpoint, cancellationToken);
        return _current;
    }

    public void End(StoryOutcome outcome)
    {
        if (outcome is not (StoryOutcome.Defeated or StoryOutcome.GameCompleted or StoryOutcome.MainMenu))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }
        Outcome = outcome;
    }

    public void RequestAutoSave(string reason)
    {
        if (CanSave)
        {
            PublishCheckpoint(session.SaveGameService.CaptureSave(), reason);
        }
    }

    internal void ObserveResult(StoryCommandResult result, GameState state)
    {
        if (ReferenceEquals(state, session.State) &&
            result.Outcome is StoryOutcome.Defeated or StoryOutcome.GameCompleted or StoryOutcome.MainMenu)
        {
            End(result.Outcome.Value);
        }
    }

    public bool IsCurrentCheckpoint(AutoSaveRequestedEvent request) =>
        !IsTerminal && ReferenceEquals(request.State, session.State) && ReferenceEquals(request.Save, _checkpoint);

    internal StoryOutcome GetOutcome(GameState state) =>
        ReferenceEquals(state, session.State) ? Outcome : StoryOutcome.StateReplaced;

    internal void Reset()
    {
        var previous = _current;
        _current = null;
        _checkpoint = null;
        Outcome = StoryOutcome.Completed;
        previous?.Cancel();
    }

    internal void Finish(SessionExecution execution, bool completed)
    {
        if (!ReferenceEquals(_current, execution)) return;
        _current = null;
        if (IsTerminal) return;

        if (!completed && ReferenceEquals(execution.State, session.State))
        {
            execution.Cancel();
            var wasStarted = session.PlayTimeService.IsStarted;
            var wasRunning = session.PlayTimeService.IsRunning;
            session.SaveGameService.LoadSave(execution.Checkpoint);
            if (wasStarted) session.PlayTimeService.Start();
            if (!wasRunning) session.PlayTimeService.Pause();
            session.Events.Publish(new ExecutionAbortedEvent());
        }
    }

    private void PublishCheckpoint(SaveGame save, string reason)
    {
        _checkpoint = save;
        session.Events.Publish(new AutoSaveRequestedEvent(reason, session.State, save));
    }
}

public sealed class SessionExecution : IDisposable
{
    private readonly SessionExecutionService _service;
    private readonly SessionExecution? _owner;
    private readonly CancellationTokenSource _cancellation;
    private bool _finished;
    private bool _aborted;

    internal SessionExecution(SessionExecutionService service, GameState state, SaveGame checkpoint, CancellationToken token)
    {
        _service = service;
        State = state;
        Checkpoint = checkpoint;
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
    }

    internal SessionExecution(SessionExecutionService service, SessionExecution owner, CancellationToken token)
    {
        _service = service;
        _owner = owner;
        State = owner.State;
        Checkpoint = owner.Checkpoint;
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(owner.Token, token);
    }

    internal GameState State { get; }
    internal SaveGame Checkpoint { get; }
    public CancellationToken Token => _cancellation.Token;
    public StoryOutcome Outcome => _service.GetOutcome(State);

    public StoryOutcome Complete()
    {
        var outcome = Outcome;
        if (!_finished)
        {
            if (_aborted) throw new InvalidOperationException("子流程未完整执行，正在恢复检查点。");
            if (outcome != StoryOutcome.StateReplaced) Token.ThrowIfCancellationRequested();
            _finished = true;
            if (_owner is null) _service.Finish(this, completed: true);
        }
        return outcome;
    }

    internal void Cancel() => _cancellation.Cancel();

    public void Dispose()
    {
        if (!_finished)
        {
            _finished = true;
            if (_owner is not null) _owner._aborted = true;
            if (_owner is null) _service.Finish(this, completed: false);
        }
        _cancellation.Dispose();
    }
}
