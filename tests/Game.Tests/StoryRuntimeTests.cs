using Game.Application;
using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Story;
using Game.StoryDsl;

namespace Game.Tests;

public sealed class StoryRuntimeTests
{
    [Fact]
    public async Task Service_ExecutesStrictVariablesBranchAndJump()
    {
        const string source = """
        # start
        quest_stage = 1
        quest_stage += 2
        if quest_stage == 3
          jump end

        # end
        主角：完成
        """;
        var script = StoryCompiler.Compile(source);
        var host = new RecordingHost();
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]), host);

        await session.StoryService.ExecuteAsync("start");

        Assert.Equal(3, session.State.Story.Variables["quest_stage"].AsNumber("test"));
        Assert.Contains("完成", host.DialogueTexts);
        Assert.True(session.State.Story.IsStoryCompleted("end"));
    }

    [Fact]
    public async Task Service_ExecutesChineseVariableIdentifiers()
    {
        const string source = """
        # 中文变量
        是否拜师 = true
        门派声望 = 9
        if 是否拜师 && 门派声望 >= 9
          完成入门任务 = true
        del 门派声望
        """;
        var script = StoryCompiler.Compile(source);
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [script]));

        await session.StoryService.ExecuteAsync("中文变量");

        Assert.True(session.State.Story.Variables["是否拜师"].AsBoolean("test"));
        Assert.True(session.State.Story.Variables["完成入门任务"].AsBoolean("test"));
        Assert.False(session.State.Story.TryGetVariable("门派声望", out _));
    }

    [Fact]
    public async Task Service_StateReplacementTerminatesOriginatingStoryWithoutPollutingReplacementState()
    {
        var script = StoryCompiler.Compile("""
        # start
        replace_state()
        polluted = true
        """);
        var originalState = new GameState();
        var replacementState = new GameState();
        var host = new StateReplacingHost(replacementState);
        var session = new GameSession(
            originalState,
            TestContentFactory.CreateRepository(storyScripts: [script]),
            host);
        host.Session = session;

        await session.StoryService.ExecuteAsync("start");

        Assert.Same(replacementState, session.State);
        Assert.False(originalState.Story.IsStoryCompleted("start"));
        Assert.False(replacementState.Story.IsStoryCompleted("start"));
        Assert.False(replacementState.Story.TryGetVariable("polluted", out _));
    }

    [Fact]
    public async Task Service_ExecutionContextIsIsolatedAndRequired()
    {
        const string source = """
        # item
        if item_target == 'hero'
          journal('ok')
        """;
        var script = StoryCompiler.Compile(source);
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]));
        var context = new StoryExecutionContext(new Dictionary<string, ExpressionValue> { ["item_target"] = ExpressionValue.FromString("hero") });

        await session.StoryService.ExecuteAsync("item", context);
        Assert.Single(session.State.Journal.Entries);
        await Assert.ThrowsAsync<ExpressionEvaluationException>(() => session.StoryService.ExecuteAsync("item"));
    }

    [Fact]
    public async Task DynamicVariablesEnforceTypeAndReservedNames()
    {
        var typeScript = StoryCompiler.Compile("""
        # type
        flag = true
        flag = 1
        """);
        var typeSession = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [typeScript]));
        await Assert.ThrowsAsync<InvalidOperationException>(() => typeSession.StoryService.ExecuteAsync("type"));

        var reservedScript = StoryCompiler.Compile("""
        # reserved
        silver = 1
        """);
        var reservedSession = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [reservedScript]));
        await Assert.ThrowsAsync<InvalidOperationException>(() => reservedSession.StoryService.ExecuteAsync("reserved"));
    }

    [Fact]
    public async Task VariableDeletionPublishesEventsOnlyWhenStateChanges()
    {
        const string source = """
        # delete
        temporary = ['a', 'b']
        del temporary
        del temporary
        """;
        var logger = new CollectingDiagnosticLogger();
        var script = StoryCompiler.Compile(source);
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [script]),
            logger);
        var changes = 0;
        using var subscription = session.Events.Subscribe<StoryStateChangedEvent>(_ => changes++);
        var events = new List<StoryEvent>();

        await foreach (var storyEvent in session.StoryService.RunAsync("delete"))
        {
            events.Add(storyEvent);
        }

        Assert.False(session.State.Story.TryGetVariable("temporary", out _));
        Assert.Equal(2, changes);
        Assert.Single(events.OfType<VariableAssignedEvent>());
        Assert.Single(events.OfType<VariableDeletedEvent>());
        Assert.Contains(logger.Entries, entry =>
            entry.Level == DiagnosticLogLevel.Warning && entry.Message.Contains("temporary", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AssignmentEvaluatesRightHandSideOnceAndCompoundFormRequiresExistingNumber()
    {
        var assignment = StoryCompiler.Compile("""
        # assign
        sampled = chance(0.5)
        """);
        var random = new RecordingRandom();
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [assignment]),
            randomService: random);

        await session.StoryService.ExecuteAsync("assign");

        Assert.Equal(1, random.DoubleCalls);
        Assert.True(session.State.Story.Variables["sampled"].AsBoolean("test"));

        var compound = StoryCompiler.Compile("""
        # compound
        missing += 1
        """);
        var compoundSession = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [compound]));
        await Assert.ThrowsAsync<ExpressionEvaluationException>(() =>
            compoundSession.StoryService.ExecuteAsync("compound"));
    }

    [Theory]
    [InlineData("set", "silver")]
    [InlineData("delete", "silver")]
    [InlineData("set", "item_target")]
    [InlineData("delete", "item_target")]
    public async Task StateStepsRejectReadOnlyVariables(string kind, string target)
    {
        var statement = kind == "set" ? $"{target} = 1" : $"del {target}";
        var script = StoryCompiler.Compile("# readonly\n" + statement);
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [script]));
        var context = target == "item_target"
            ? new StoryExecutionContext(new Dictionary<string, ExpressionValue>
            {
                ["item_target"] = ExpressionValue.FromString("hero"),
            })
            : StoryExecutionContext.Empty;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.StoryService.ExecuteAsync("readonly", context));
    }

    [Fact]
    public void ReplaceState_RejectsReservedVariablesWithoutReplacingTheCurrentState()
    {
        var original = new GameState();
        var session = new GameSession(original, TestContentFactory.CreateRepository());
        var invalid = new GameState();
        invalid.Story.SetVariable("silver", ExpressionValue.FromNumber(1));

        Assert.Throws<InvalidOperationException>(() => session.ReplaceState(invalid));
        Assert.Same(original, session.State);
    }

    [Fact]
    public async Task Service_ResolvesJumpAndCallTargetsAcrossStoryFiles()
    {
        var first = StoryCompiler.Compile("""
        # start
        call shared
        jump finish
        """, "first.story");
        var second = StoryCompiler.Compile("""
        # shared
        主角：共享片段
        return

        # finish
        主角：结束
        """, "second.story");
        var host = new RecordingHost();
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [first, second]), host);

        await session.StoryService.ExecuteAsync("start");

        Assert.Equal(["共享片段", "结束"], host.DialogueTexts);
        Assert.True(session.State.Story.IsStoryCompleted("shared"));
        Assert.True(session.State.Story.IsStoryCompleted("finish"));
    }

    [Fact]
    public async Task Runtime_CallReturnsThroughNestedBranchAndChoicePreservesSourceIndex()
    {
        var script = StoryCompiler.Compile("""
        # start
        call sub
        主角：选择
        - 隐藏 if false
        - 可见
          主角：选择完成

        # sub
        if true
          return
        主角：不应执行
        """);
        var host = new RecordingHost { SelectedOptionIndex = 1 };
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]), host);

        await session.StoryService.ExecuteAsync("start");

        Assert.Equal(["选择完成"], host.DialogueTexts);
        Assert.Equal([1], host.OfferedOptionIndices);
    }

    [Fact]
    public async Task Runtime_RejectsChoiceWithoutAnyVisibleOption()
    {
        var script = StoryCompiler.Compile("""
        # start
        主角：无路可走
        - 隐藏 if false
        """);
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]));

        var exception = await Assert.ThrowsAsync<StoryRuntimeException>(() =>
            session.StoryService.ExecuteAsync("start"));
        Assert.Contains("no available options", exception.Message);
    }

    [Fact]
    public async Task Runtime_MixesChoiceBlocksAndEvaluatesOnlyVisitedConditionsOnce()
    {
        var script = StoryCompiler.Compile("""
        # start
        主角：选择
        - 隐藏 if chance(0)
        - 普通
        if chance(1)
          - 首个分支 if chance(1)
        elif chance(1)
          - 未访问分支 if chance(1)
        else
          - 回退 if chance(1)
        if chance(1)
          - 独立分支
        """);
        var random = new RecordingRandom();
        var host = new RecordingHost { SelectedOptionIndex = 1 };
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [script]),
            host,
            randomService: random);

        await session.StoryService.ExecuteAsync("start");

        Assert.Equal([1, 2, 5], host.OfferedOptionIndices);
        Assert.Equal(4, random.DoubleCalls);
    }

    [Theory]
    [InlineData(BattleOutcome.Win, false, true)]
    [InlineData(BattleOutcome.Lose, true, false)]
    public async Task Runtime_HandlesBattleOutcomesWithoutExplicitBranches(
        BattleOutcome outcome,
        bool expectedGameOver,
        bool expectedContinuation)
    {
        var script = StoryCompiler.Compile("""
        # start
        battle test
        主角：继续
        """);
        var host = new RecordingHost { BattleOutcome = outcome };
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]), host);

        await session.StoryService.ExecuteAsync("start");

        Assert.Equal(expectedGameOver, host.GameOverInvoked);
        Assert.Equal(expectedContinuation, host.DialogueTexts.Contains("继续"));
    }

    private sealed class RecordingHost : IRuntimeHost
    {
        public List<string> DialogueTexts { get; } = [];
        public List<int> OfferedOptionIndices { get; } = [];
        public int SelectedOptionIndex { get; init; }
        public BattleOutcome BattleOutcome { get; init; } = BattleOutcome.Win;
        public bool GameOverInvoked { get; private set; }
        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) { DialogueTexts.Add(dialogue.Text); return ValueTask.CompletedTask; }
        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken)
        {
            OfferedOptionIndices.AddRange(choice.Options.Select(static option => option.Index));
            return ValueTask.FromResult(SelectedOptionIndex);
        }
        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) => ValueTask.FromResult(BattleOutcome);
        public ValueTask GameOverAsync(CancellationToken cancellationToken) { GameOverInvoked = true; return ValueTask.CompletedTask; }
    }

    private sealed class StateReplacingHost(GameState replacementState) : IRuntimeHost
    {
        public GameSession Session { get; set; } = null!;

        [StoryCommand("replace_state")]
        public void ReplaceState() => Session.ReplaceState(replacementState);

        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) =>
            ValueTask.FromResult(0);

        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
            ValueTask.FromResult(BattleOutcome.Win);
    }

    private sealed class CollectingDiagnosticLogger : IDiagnosticLogger
    {
        private readonly List<(DiagnosticLogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(DiagnosticLogLevel Level, string Message)> Entries => _entries;

        public void Log(DiagnosticLogLevel level, string message, Exception? exception = null) =>
            _entries.Add((level, message));
    }

    private sealed class RecordingRandom : IRandomService
    {
        public int DoubleCalls { get; private set; }

        public double NextDouble()
        {
            DoubleCalls++;
            return 0.25;
        }

        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
