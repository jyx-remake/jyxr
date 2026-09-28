using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Story;
using Game.Expressions;

namespace Game.Tests;

public sealed class WorldTriggerServiceTests
{
    [Fact]
    public async Task PendingAndExecutingEventsAreNotSavedAsCompleted_AndNestedRequestsDoNotReenter()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new TriggerHost { Execute = token => new ValueTask(release.Task.WaitAsync(token)) };
        var session = CreateSession(host);
        var service = session.WorldTriggerService;
        var autoSaves = 0;
        using var subscription = session.Events.Subscribe<AutoSaveRequestedEvent>(_ => autoSaves++);

        service.RequestCheck();
        service.RequestCheck();
        Assert.Empty(session.State.WorldTriggers.ToRecord().CompletedTriggerIds);
        var execution = service.ExecutePendingAsync();
        Assert.False(execution.IsCompleted);
        Assert.Empty(session.State.WorldTriggers.ToRecord().CompletedTriggerIds);
        service.RequestCheck(); // A map command inside the event.
        Assert.False(await service.ExecutePendingAsync());
        Assert.Equal(1, host.Calls);

        release.SetResult();
        Assert.True(await execution);
        Assert.True(session.State.WorldTriggers.IsCompleted("global"));
        Assert.Equal(1, autoSaves);
        Assert.False(service.HasPendingCheck);
        service.RequestCheck();
        Assert.False(await service.ExecutePendingAsync());
        Assert.Equal(1, host.Calls);
    }

    [Fact]
    public async Task FailedEventRemainsEligibleOnNextRequest()
    {
        var host = new TriggerHost { Execute = _ => throw new InvalidOperationException("host failed") };
        var session = CreateSession(host);
        session.WorldTriggerService.RequestCheck();
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.WorldTriggerService.ExecutePendingAsync());
        Assert.False(session.State.WorldTriggers.IsCompleted("global"));
        Assert.False(session.WorldTriggerService.HasPendingCheck); // No automatic failure loop.

        host.Execute = _ => ValueTask.CompletedTask;
        session.WorldTriggerService.RequestCheck();
        Assert.True(await session.WorldTriggerService.ExecutePendingAsync());
        Assert.Equal(2, host.Calls);
    }

    [Fact]
    public async Task CancellationDoesNotConsumeEvent()
    {
        var host = new TriggerHost { Execute = token => new ValueTask(Task.Delay(Timeout.Infinite, token)) };
        var session = CreateSession(host);
        using var cancellation = new CancellationTokenSource();
        session.WorldTriggerService.RequestCheck();
        var execution = session.WorldTriggerService.ExecutePendingAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.Empty(session.State.WorldTriggers.CompletedTriggerIds);
        host.Execute = _ => ValueTask.CompletedTask;
        session.WorldTriggerService.RequestCheck();
        Assert.True(await session.WorldTriggerService.ExecutePendingAsync());
    }

    [Fact]
    public async Task ReplacingStateDuringExecutionDoesNotCompleteEventOrSaveEitherState()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new TriggerHost { Execute = _ => new ValueTask(release.Task) };
        var session = CreateSession(host);
        var previousState = session.State;
        var autoSaves = 0;
        using var subscription = session.Events.Subscribe<AutoSaveRequestedEvent>(_ => autoSaves++);
        session.WorldTriggerService.RequestCheck();
        var execution = session.WorldTriggerService.ExecutePendingAsync();
        session.ReplaceState(new GameState());
        session.WorldTriggerService.RequestCheck();

        release.SetResult();
        Assert.False(await execution);
        Assert.Empty(previousState.WorldTriggers.CompletedTriggerIds);
        Assert.Empty(session.State.WorldTriggers.CompletedTriggerIds);
        Assert.Equal(0, autoSaves);
        Assert.True(session.WorldTriggerService.HasPendingCheck);
        Assert.True(await session.WorldTriggerService.ExecutePendingAsync());
        Assert.Equal(2, host.Calls);
    }

    [Fact]
    public async Task PendingCheckBelongsToTheStateThatRequestedIt()
    {
        var host = new TriggerHost();
        var session = CreateSession(host);
        session.WorldTriggerService.RequestCheck();
        session.ReplaceState(new GameState());

        Assert.False(session.WorldTriggerService.HasPendingCheck);
        Assert.False(await session.WorldTriggerService.ExecutePendingAsync());
        Assert.Equal(0, host.Calls);
    }

    [Fact]
    public async Task RepeatableEventRunsOncePerExternalRequest()
    {
        var host = new TriggerHost();
        var session = CreateSession(host, RepeatMode.Infinite);
        host.Execute = _ =>
        {
            session.WorldTriggerService.RequestCheck();
            return ValueTask.CompletedTask;
        };

        for (var i = 0; i < 2; i++)
        {
            session.WorldTriggerService.RequestCheck();
            Assert.True(await session.WorldTriggerService.ExecutePendingAsync());
            Assert.False(session.WorldTriggerService.HasPendingCheck);
            Assert.Empty(session.State.WorldTriggers.CompletedTriggerIds);
        }
        Assert.Equal(2, host.Calls);
    }

    [Fact]
    public async Task BlockedEventsAreNotExecutedOrConsumed()
    {
        var host = new TriggerHost();
        var session = CreateSession(host);
        session.WorldTriggerService.Block();
        session.WorldTriggerService.RequestCheck();
        Assert.False(await session.WorldTriggerService.ExecutePendingAsync());
        Assert.Equal(0, host.Calls);
        Assert.True(session.WorldTriggerService.HasPendingCheck);
        session.WorldTriggerService.Unblock();
        Assert.True(await session.WorldTriggerService.ExecutePendingAsync());
    }

    private static GameSession CreateSession(TriggerHost host, RepeatMode repeatMode = RepeatMode.Once) =>
        new(new GameState(), TestContentFactory.CreateRepository(worldTriggers:
        [
            new WorldTriggerDefinition
            {
                Id = "global",
                Action = new ExpressionParser().ParseCall("probe()"),
                RepeatMode = repeatMode,
            },
        ]), host);

    private sealed class TriggerHost : IRuntimeHost
    {
        public Func<CancellationToken, ValueTask> Execute { get; set; } = _ => ValueTask.CompletedTask;
        public int Calls { get; private set; }

        [StoryCommand("probe")]
        private ValueTask ProbeAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Execute(cancellationToken);
        }

        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
