using System.Text.Json;
using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Serialization;
using Game.Core.Story;
using Game.Expressions;
using Game.StoryDsl;

namespace Game.Tests;

public sealed class SessionCheckpointTests
{
    [Theory]
    [InlineData("battle test", StoryOutcome.Defeated)]
    [InlineData("huashan()", StoryOutcome.Defeated)]
    [InlineData("stop()", StoryOutcome.Defeated)]
    [InlineData("game_over()", StoryOutcome.Defeated)]
    [InlineData("game_complete()", StoryOutcome.GameCompleted)]
    [InlineData("main_menu()", StoryOutcome.MainMenu)]
    public async Task TerminalFlowPreservesEntranceCheckpoint(string command, StoryOutcome outcome)
    {
        var source = "# entry\njump challenge\n# challenge\nchange_silver(7)\n" + command + "\nchange_silver(100)";
        var (session, host) = CreateSession(source);
        var requests = CollectCheckpoints(session);
        var clockBefore = session.State.Clock.ToRecord();
        var location = session.MapService.GetCurrentMap().Locations.Single();
        using var interaction = session.MapService.InteractWithLocation(location);

        Assert.False(await session.MapService.ExecuteInteractionAsync(interaction));

        Assert.Equal(outcome, session.Execution.Outcome);
        Assert.Equal(20, session.State.Currency.Silver);
        Assert.False(session.State.MapEventProgress.IsCompleted("map", "door", "event"));
        Assert.Throws<InvalidOperationException>(() => session.SaveGameService.CreateSave());
        var checkpoint = Assert.Single(requests);
        Assert.Equal(13, checkpoint.Save.Currency.Silver);
        Assert.Equal(clockBefore, checkpoint.Save.Clock);
        Assert.False(checkpoint.Save.RestoreStoryState().IsStoryCompleted("entry"));
        Assert.False(checkpoint.Save.RestoreMapEventProgress().IsCompleted("map", "door", "event"));
        Assert.False(session.MapService.ReturnToMap(session.State, "terminal"));
        session.Execution.RequestAutoSave("terminal");
        Assert.Single(requests);
        if (command == "huashan()") Assert.Equal(2, host.BattleCount);

        session.SaveGameService.LoadSave(RoundTrip(checkpoint.Save));
        Assert.True(session.Execution.CanSave);
        Assert.NotNull(session.MapService.GetCurrentMap().Locations.Single().Event);
        Assert.False(session.Execution.IsCurrentCheckpoint(checkpoint));
    }

    [Fact]
    public async Task ExplicitLoseBranchCanCompleteAndCommit()
    {
        var (session, _) = CreateSession("""
            # entry
            battle test
            - lose
              change_silver(5)
            """);
        var requests = CollectCheckpoints(session);
        using var interaction = session.MapService.InteractWithLocation(session.MapService.GetCurrentMap().Locations.Single());

        Assert.True(await session.MapService.ExecuteInteractionAsync(interaction));

        Assert.Equal(2, requests.Count);
        Assert.Equal(18, requests[^1].Save.Currency.Silver);
        Assert.True(requests[^1].Save.RestoreMapEventProgress().IsCompleted("map", "door", "event"));
        Assert.True(session.Execution.IsCurrentCheckpoint(requests[^1]));
        Assert.False(session.Execution.IsCurrentCheckpoint(requests[0]));
        Assert.True(session.Execution.CanSave);
    }

    [Fact]
    public async Task NestedStoryTerminationStopsItsCaller()
    {
        var (session, _) = CreateSession("""
            # entry
            story('child')
            change_silver(100)
            # child
            game_complete()
            change_silver(1000)
            """);
        var requests = CollectCheckpoints(session);
        using var interaction = session.MapService.InteractWithLocation(session.MapService.GetCurrentMap().Locations.Single());

        Assert.False(await session.MapService.ExecuteInteractionAsync(interaction));

        Assert.Equal(StoryOutcome.GameCompleted, session.Execution.Outcome);
        Assert.Equal(13, session.State.Currency.Silver);
        Assert.False(session.State.Story.IsStoryCompleted("entry"));
        Assert.False(session.State.Story.IsStoryCompleted("child"));
        Assert.Single(requests);
        Assert.Equal(1, session.Profile.CompletionCount);
    }

    [Fact]
    public async Task FailureRestoresStateAndPlayTimeBeforeReopeningSaving()
    {
        var (session, _) = CreateSession("""
            # entry
            jump work
            # work
            change_silver(7)
            fail()
            """);
        session.PlayTimeService.Start();
        var requests = CollectCheckpoints(session);
        var before = session.State.Clock.ToRecord();
        using var interaction = session.MapService.InteractWithLocation(session.MapService.GetCurrentMap().Locations.Single());
        Assert.Throws<InvalidOperationException>(() => session.SaveGameService.CreateSave());

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.MapService.ExecuteInteractionAsync(interaction));

        Assert.Equal(13, session.State.Currency.Silver);
        Assert.Equal(before, session.State.Clock.ToRecord());
        Assert.Empty(session.State.Story.CompletedStoryIds);
        Assert.False(session.State.MapEventProgress.IsCompleted("map", "door", "event"));
        Assert.True(session.Execution.CanSave);
        Assert.True(session.PlayTimeService.IsStarted);
        Assert.True(session.PlayTimeService.IsRunning);
        Assert.Single(requests);
        Assert.NotNull(session.SaveGameService.CreateSave());
    }

    [Fact]
    public async Task CancellationRestoresPartialChanges()
    {
        var (session, host) = CreateSession("# entry\nchange_silver(7)\npause()");
        host.Wait = token => new ValueTask(Task.Delay(Timeout.Infinite, token));
        using var cancellation = new CancellationTokenSource();
        using var interaction = session.MapService.InteractWithLocation(session.MapService.GetCurrentMap().Locations.Single());
        var execution = session.MapService.ExecuteInteractionAsync(interaction, cancellation.Token);
        Assert.False(execution.IsCompleted);
        Assert.Equal(20, session.State.Currency.Silver);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);

        Assert.Equal(13, session.State.Currency.Silver);
        Assert.True(session.Execution.CanSave);
    }

    [Fact]
    public async Task StateReplacementInvalidatesOldContinuationAndCheckpoint()
    {
        var (session, host) = CreateSession("# entry\npause()\nchange_silver(100)");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Wait = _ => new ValueTask(release.Task);
        var requests = CollectCheckpoints(session);
        var replacement = session.SaveGameService.CreateSave();
        using var interaction = session.MapService.InteractWithLocation(session.MapService.GetCurrentMap().Locations.Single());
        var execution = session.MapService.ExecuteInteractionAsync(interaction);

        session.SaveGameService.LoadSave(replacement);
        release.SetResult();

        Assert.False(await execution);
        Assert.Equal(13, session.State.Currency.Silver);
        Assert.False(session.State.MapEventProgress.IsCompleted("map", "door", "event"));
        Assert.Single(requests);
        Assert.False(session.Execution.IsCurrentCheckpoint(requests[0]));
    }

    [Theory]
    [InlineData("story('entry')", StoryOutcome.GameCompleted)]
    [InlineData("stop()", StoryOutcome.Defeated)]
    public async Task GlobalTerminalEventRemainsUncompleted(string action, StoryOutcome outcome)
    {
        var trigger = new WorldTriggerDefinition
        {
            Id = "ending",
            RepeatMode = RepeatMode.Once,
            Action = new ExpressionParser().ParseCall(action, "test"),
        };
        var (session, _) = CreateSession("# entry\ngame_complete()", [trigger]);
        var requests = CollectCheckpoints(session);

        Assert.False(await session.WorldTriggerService.ExecutePendingAsync());

        Assert.Equal(outcome, session.Execution.Outcome);
        Assert.False(session.State.WorldTriggers.IsCompleted("ending"));
        Assert.Single(requests);
    }

    [Fact]
    public async Task NewStateOpeningCommitsIndependentlyOfItsOldCaller()
    {
        var (session, _) = CreateSession("""
            # entry
            restart_flow()
            change_silver(1000)
            # opening
            change_silver(30)
            enter_map()
            """);
        var requests = CollectCheckpoints(session);
        using var interaction = session.MapService.InteractWithLocation(session.MapService.GetCurrentMap().Locations.Single());

        Assert.False(await session.MapService.ExecuteInteractionAsync(interaction));

        Assert.Equal(30, session.State.Currency.Silver);
        Assert.True(session.Execution.CanSave);
        Assert.Equal(2, requests.Count);
        Assert.Equal(13, requests[0].Save.Currency.Silver);
        Assert.Equal(30, requests[1].Save.Currency.Silver);
        Assert.False(session.Execution.IsCurrentCheckpoint(requests[0]));
        Assert.True(session.Execution.IsCurrentCheckpoint(requests[1]));
        Assert.False(session.State.MapEventProgress.IsCompleted("map", "door", "event"));
    }

    [Fact]
    public async Task SuccessfulHuashanStagesShareTheirEntranceCheckpoint()
    {
        var (session, host) = CreateSession("""
            # entry
            huashan()
            # original_华山论剑分枝判断
            game_complete()
            """);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Battle = (index, _) => index == 1 ? ValueTask.FromResult(true) : new ValueTask<bool>(release.Task);
        var requests = CollectCheckpoints(session);
        using var interaction = session.MapService.InteractWithLocation(session.MapService.GetCurrentMap().Locations.Single());
        var execution = session.MapService.ExecuteInteractionAsync(interaction);
        Assert.False(execution.IsCompleted);
        Assert.Equal(2, host.BattleCount);
        Assert.Single(requests);
        Assert.Throws<InvalidOperationException>(() => session.SaveGameService.CreateSave());

        release.SetResult(true);

        Assert.False(await execution);
        Assert.Equal(StoryOutcome.GameCompleted, session.Execution.Outcome);
        Assert.Single(requests);
        Assert.False(requests[0].Save.RestoreStoryState().IsStoryCompleted("entry"));
    }

    [Fact]
    public void AbandonedNestedExecutionPreventsParentCommit()
    {
        var (session, _) = CreateSession("# entry\nchange_silver(1)");
        using (var parent = session.Execution.Begin("parent"))
        {
            using (session.Execution.Begin("child"))
            {
                session.State.Currency.AddSilver(7);
            }
            Assert.Throws<InvalidOperationException>(() => parent.Complete());
        }

        Assert.Equal(13, session.State.Currency.Silver);
        Assert.True(session.Execution.CanSave);
    }

    [Fact]
    public async Task ExpiredTimedStorySurvivesSaveLoadUntilExecutionCommits()
    {
        var (session, _) = CreateSession("# timeout\nchange_silver(5)");
        session.State.Story.SetTimeKey("timer", session.State.Clock, 1, "timeout");
        session.State.Clock.AdvanceDays(2);
        var expired = Assert.Single(session.StoryTimeKeyExpirationService.CheckExpired());
        Assert.True(session.State.Story.HasTimeKey("timer"));
        var saved = RoundTrip(session.SaveGameService.CreateSave());
        session.SaveGameService.LoadSave(saved);
        Assert.False(await session.StoryTimeKeyExpirationService.ExecuteAsync(expired));
        var requests = CollectCheckpoints(session);

        Assert.True(await session.StoryTimeKeyExpirationService.ExecuteAsync(
            Assert.Single(session.StoryTimeKeyExpirationService.CheckExpired())));

        Assert.False(session.State.Story.HasTimeKey("timer"));
        Assert.Equal(18, session.State.Currency.Silver);
        Assert.Equal(2, requests.Count);
        Assert.True(requests[0].Save.RestoreStoryState().HasTimeKey("timer"));
        Assert.False(requests[1].Save.RestoreStoryState().HasTimeKey("timer"));
    }

    [Fact]
    public async Task FailedTimedStoryRestoresItsPendingKey()
    {
        var (session, _) = CreateSession("# timeout\nchange_silver(5)\nfail()");
        session.State.Story.SetTimeKey("timer", session.State.Clock, 1, "timeout");
        session.State.Clock.AdvanceDays(2);
        var expired = Assert.Single(session.StoryTimeKeyExpirationService.CheckExpired());

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.StoryTimeKeyExpirationService.ExecuteAsync(expired));

        Assert.Equal(13, session.State.Currency.Silver);
        Assert.Single(session.StoryTimeKeyExpirationService.CheckExpired());
        Assert.True(session.Execution.CanSave);
    }

    private static SaveGame RoundTrip(SaveGame save) =>
        JsonSerializer.Deserialize<SaveGame>(JsonSerializer.Serialize(save, GameJson.Default), GameJson.Default)!;

    private static List<AutoSaveRequestedEvent> CollectCheckpoints(GameSession session)
    {
        var requests = new List<AutoSaveRequestedEvent>();
        session.Events.Subscribe<AutoSaveRequestedEvent>(requests.Add);
        return requests;
    }

    private static (GameSession Session, Host Host) CreateSession(string source, IReadOnlyList<WorldTriggerDefinition>? triggers = null)
    {
        var map = new MapDefinition
        {
            Id = "map",
            Name = "map",
            Kind = MapKind.Small,
            Locations = [new MapLocationDefinition
            {
                Id = "door",
                Events = [new MapEventDefinition
                {
                    Id = "event",
                    RepeatMode = RepeatMode.Once,
                    Action = new ExpressionParser().ParseCall("story('entry')", "test"),
                    When = new ExpressionParser().ParseExpression("!story_completed('entry')", "test"),
                }],
            }],
        };
        var tower = new TowerDefinition
        {
            Id = "华山论剑",
            Name = "华山论剑",
            Stages = [
                new TowerStageDefinition { Id = "one", BattleId = "one", Index = 0 },
                new TowerStageDefinition { Id = "two", BattleId = "two", Index = 1 },
            ],
        };
        var host = new Host();
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(
            maps: [map], towers: [tower], storyScripts: [StoryCompiler.Compile(source)], worldTriggers: triggers), host);
        host.Session = session;
        session.State.Currency.AddSilver(13);
        session.MapService.EnterMap("map");
        return (session, host);
    }

    private sealed class Host : IRuntimeHost, ISpecialBattleRuntimeHost
    {
        public GameSession Session { get; set; } = null!;
        public int BattleCount { get; private set; }
        public Func<CancellationToken, ValueTask> Wait { get; set; } = _ => ValueTask.CompletedTask;
        public Func<int, CancellationToken, ValueTask<bool>>? Battle { get; set; }

        [StoryCommand("story")]
        private async ValueTask<StoryCommandResult> StoryAsync(string storyId, CancellationToken cancellationToken)
        {
            var outcome = await Session.StoryService.ExecuteAsync(storyId, cancellationToken: cancellationToken);
            return outcome == StoryOutcome.Completed ? StoryCommandResult.None : StoryCommandResult.End(outcome);
        }

        [StoryCommand("pause")]
        private ValueTask PauseAsync(CancellationToken cancellationToken) => Wait(cancellationToken);

        [StoryCommand("fail")]
        private void Fail() => throw new InvalidOperationException("diagnostic failure");

        [StoryCommand("stop")]
        private StoryCommandResult Stop() => StoryCommandResult.End(StoryOutcome.Defeated);

        [StoryCommand("restart_flow")]
        private async ValueTask RestartFlowAsync()
        {
            Session.ReplaceState(new GameState());
            var outcome = await Session.StoryService.ExecuteAsync("opening");
            if (outcome == StoryOutcome.Completed) Session.Execution.RequestAutoSave("opening completed");
        }

        [StoryCommand("enter_map")]
        private void EnterMap() => Session.MapService.EnterMap("map");

        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) => ValueTask.FromResult(0);
        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) => ValueTask.FromResult(BattleOutcome.Lose);
        public ValueTask<IReadOnlyList<string>> SelectCombatantsAsync(CombatantSelectionRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<string>>([$"fighter_{BattleCount}"]);
        public ValueTask<bool> RunBattleAsync(SpecialBattleRequest request, CancellationToken cancellationToken)
        {
            BattleCount++;
            return Battle is null ? ValueTask.FromResult(BattleCount == 1) : Battle(BattleCount, cancellationToken);
        }
    }
}
