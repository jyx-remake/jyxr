using System.Text.Json.Nodes;
using Game.Application;
using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Tests;

public sealed class StoryV3Tests
{
    [Fact]
    public void JsonParser_PreservesDirectDialoguePortraitOverride()
    {
        var script = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"dialogue","speaker":"女子","portrait":"头像.女主1","text":"公子难道忘了吗？"},
          {"kind":"dialogue","speaker":"主角","text":"没有。"}
        ]}]}
        """, "direct-dialogue-portrait");

        var first = Assert.IsType<DialogueStep>(script.Segments[0].Steps[0]);
        Assert.Equal("女子", first.Speaker);
        Assert.Equal("头像.女主1", first.Portrait);
        var second = Assert.IsType<DialogueStep>(script.Segments[0].Steps[1]);
        Assert.Null(second.Portrait);
    }

    [Fact]
    public void JsonParser_JsonObjectAndStringEntrypointsProduceEquivalentIr()
    {
        const string json = """
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"set","target":"quest_stage","value":"3"},
          {"kind":"choice","prompt":{"speaker":"主角","text":"走吗"},"style":"bold","blocks":[
            {"kind":"branch","cases":[{"when":"quest_stage >= 3","options":[{"text":"走","when":"true","steps":[]}]}],"fallback":null}
          ]},
          {"kind":"battle","battleId":"sample_battle","outcomes":{"win":[],"lose":[],"timeout":[]}},
          {"kind":"branch","cases":[{"when":"quest_stage == 3","steps":[]}],"fallback":[]}
        ]}]}
        """;
        var root = JsonNode.Parse(json)!.AsObject();

        var fromString = StoryScriptJson.Parse(json, "equivalent-story");
        var fromNode = StoryScriptJson.Parse(root, "equivalent-story");

        Assert.Equivalent(fromString, fromNode, strict: true);
    }

    [Fact]
    public void JsonParser_PreservesLegacyBattleOptionsSeparatelyFromCatalogId()
    {
        var script = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"battle","battleId":"legacy_battle","totalBattles":2,"battleLevel":5,"outcomes":{}}
        ]}]}
        """, "legacy-battle-options");

        var battle = Assert.IsType<BattleStep>(Assert.Single(script.Segments[0].Steps));
        Assert.Equal("legacy_battle", battle.BattleId);
        Assert.Equal(2, battle.TotalBattles);
        Assert.Equal(5, battle.BattleLevel);
    }

    [Fact]
    public void JsonParser_JsonObjectEntrypointIncludesSourceNameInExpressionErrors()
    {
        var root = JsonNode.Parse("""
            {"version":3,"segments":[{"name":"start","steps":[
              {"kind":"set","target":"quest_stage","value":"("}
            ]}]}
            """)!.AsObject();

        var exception = Assert.Throws<StoryRuntimeException>(() =>
            StoryScriptJson.Parse(root, "stories/invalid.story.json"));

        Assert.Contains("stories/invalid.story.json", exception.Message);
    }

    [Fact]
    public void JsonParser_ParsesV3StateStepsBranchAndChoice()
    {
        const string json = """
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"set","target":"quest_stage","value":"3"},
          {"kind":"delete","target":"obsolete_flag"},
          {"kind":"branch","cases":[{"when":"quest_stage >= 3","steps":[]}]},
          {"kind":"choice","prompt":{"speaker":"主角","text":"走吗"},"blocks":[{"kind":"branch","cases":[{"when":"has_var('quest_stage')","options":[{"text":"走","steps":[]}]}],"fallback":null}]}
        ]}]}
        """;

        var script = StoryScriptJson.Parse(json, "v3-test");

        Assert.Equal(3, script.Version);
        Assert.IsType<SetVariableStep>(script.Segments[0].Steps[0]);
        Assert.IsType<DeleteVariableStep>(script.Segments[0].Steps[1]);
        Assert.IsType<BranchStep>(script.Segments[0].Steps[2]);
        var choice = Assert.IsType<ChoiceStep>(script.Segments[0].Steps[3]);
        var block = Assert.IsType<ChoiceBranchBlock>(Assert.Single(choice.Blocks));
        Assert.Null(block.Fallback);
        Assert.Single(block.Cases);
    }

    [Theory]
    [InlineData("{\"version\":2,\"segments\":[]}")]
    [InlineData("{\"version\":3,\"segments\":[{\"name\":\"x\",\"steps\":[{\"kind\":\"command\",\"name\":\"journal\",\"args\":[]}]}]}")]
    [InlineData("{\"version\":3,\"segments\":[{\"name\":\"x\",\"steps\":[{\"kind\":\"command\",\"call\":[\"journal\"]}]}]}")]
    public void JsonParser_RejectsV2AndOldCommandShapes(string json) =>
        Assert.Throws<StoryRuntimeException>(() => StoryScriptJson.Parse(json, "invalid-story"));

    [Fact]
    public async Task Service_ExecutesStrictVariablesBranchChoiceAndJump()
    {
        const string json = """
        {"version":3,"segments":[
          {"name":"start","steps":[
            {"kind":"set","target":"quest_stage","value":"1"},
            {"kind":"set","target":"quest_stage","value":"quest_stage + (2)"},
            {"kind":"branch","cases":[{"when":"quest_stage == 3","steps":[{"kind":"jump","target":"end"}]}]}
          ]},
          {"name":"end","steps":[{"kind":"dialogue","speaker":"主角","text":"完成"}]}
        ]}
        """;
        var script = StoryScriptJson.Parse(json);
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
        const string json = """
        {"version":3,"segments":[{"name":"中文变量","steps":[
          {"kind":"set","target":"是否拜师","value":"true"},
          {"kind":"set","target":"门派声望","value":"9"},
          {"kind":"branch","cases":[{"when":"是否拜师 && 门派声望 >= 9","steps":[
            {"kind":"set","target":"完成入门任务","value":"true"}
          ]}]},
          {"kind":"delete","target":"门派声望"}
        ]}]}
        """;
        var script = StoryScriptJson.Parse(json);
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
        var script = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"command","call":"replace_state()"},
          {"kind":"set","target":"polluted","value":"true"}
        ]}]}
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
        const string json = """
        {"version":3,"segments":[{"name":"item","steps":[{"kind":"branch","cases":[{"when":"item_target == 'hero'","steps":[{"kind":"command","call":"journal('ok')"}]}]}]}]}
        """;
        var script = StoryScriptJson.Parse(json);
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]));
        var context = new StoryExecutionContext(new Dictionary<string, ExpressionValue> { ["item_target"] = ExpressionValue.FromString("hero") });

        await session.StoryService.ExecuteAsync("item", context);
        Assert.Single(session.State.Journal.Entries);
        await Assert.ThrowsAsync<ExpressionEvaluationException>(() => session.StoryService.ExecuteAsync("item"));
    }

    [Fact]
    public async Task Runtime_SkipsCommandFailuresAfterHostNotification()
    {
        var script = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"command","call":"legacy_upgrade('x')"},
          {"kind":"dialogue","speaker":"主角","text":"继续"}
        ]}]}
        """);
        var host = new RecordingHost { ContinueOnCommandFailure = true };
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]), host);

        var events = new List<StoryEvent>();
        await foreach (var storyEvent in session.StoryService.RunAsync("start"))
        {
            events.Add(storyEvent);
        }

        Assert.Equal(["legacy_upgrade"], host.CommandFailures.Select(static failure => failure.Name));
        Assert.Contains("继续", host.DialogueTexts);
        Assert.Single(events.OfType<CommandFailedEvent>());
        Assert.True(session.State.Story.IsStoryCompleted("start"));
    }

    [Fact]
    public async Task Runtime_TerminatingCommandStopsStoryWithoutMarkingSegmentCompleted()
    {
        var script = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"command","call":"terminate_story()"},
          {"kind":"dialogue","speaker":"主角","text":"不应继续"}
        ]}]}
        """);
        var host = new RecordingHost();
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]), host);

        var events = new List<StoryEvent>();
        await foreach (var storyEvent in session.StoryService.RunAsync("start"))
        {
            events.Add(storyEvent);
        }

        Assert.Empty(host.DialogueTexts);
        Assert.False(session.State.Story.IsStoryCompleted("start"));
        Assert.Single(events.OfType<StoryTerminatedEvent>());
    }

    [Fact]
    public async Task DynamicVariablesEnforceTypeAndReservedNames()
    {
        var typeScript = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"type","steps":[
          {"kind":"set","target":"flag","value":"true"},
          {"kind":"set","target":"flag","value":"1"}
        ]}]}
        """);
        var typeSession = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [typeScript]));
        await Assert.ThrowsAsync<InvalidOperationException>(() => typeSession.StoryService.ExecuteAsync("type"));

        var reservedScript = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"reserved","steps":[
          {"kind":"set","target":"silver","value":"1"}
        ]}]}
        """);
        var reservedSession = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [reservedScript]));
        await Assert.ThrowsAsync<InvalidOperationException>(() => reservedSession.StoryService.ExecuteAsync("reserved"));
    }

    [Fact]
    public async Task VariableDeletionPublishesEventsOnlyWhenStateChanges()
    {
        const string json = """
        {"version":3,"segments":[{"name":"delete","steps":[
          {"kind":"set","target":"temporary","value":"['a', 'b']"},
          {"kind":"delete","target":"temporary"},
          {"kind":"delete","target":"temporary"}
        ]}]}
        """;
        var logger = new CollectingDiagnosticLogger();
        var script = StoryScriptJson.Parse(json);
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
        var assignment = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"assign","steps":[
          {"kind":"set","target":"sampled","value":"chance(0.5)"}
        ]}]}
        """);
        var random = new RecordingRandom();
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [assignment]),
            randomService: random);

        await session.StoryService.ExecuteAsync("assign");

        Assert.Equal(1, random.DoubleCalls);
        Assert.True(session.State.Story.Variables["sampled"].AsBoolean("test"));

        var compound = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"compound","steps":[
          {"kind":"set","target":"missing","value":"missing + (1)"}
        ]}]}
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
        var step = kind == "set"
            ? $$"""{"kind":"set","target":"{{target}}","value":"1"}"""
            : $$"""{"kind":"delete","target":"{{target}}"}""";
        var script = StoryScriptJson.Parse($$"""
        {"version":3,"segments":[{"name":"readonly","steps":[{{step}}]}]}
        """);
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
        var first = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"call","target":"shared"},
          {"kind":"jump","target":"finish"}
        ]}]}
        """, "first.story.json");
        var second = StoryScriptJson.Parse("""
        {"version":3,"segments":[
          {"name":"shared","steps":[{"kind":"dialogue","speaker":"主角","text":"共享片段"},{"kind":"return"}]},
          {"name":"finish","steps":[{"kind":"dialogue","speaker":"主角","text":"结束"}]}
        ]}
        """, "second.story.json");
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
        var script = StoryScriptJson.Parse("""
        {"version":3,"segments":[
          {"name":"start","steps":[
            {"kind":"call","target":"sub"},
            {"kind":"choice","prompt":{"speaker":"主角","text":"选择"},"blocks":[
              {"kind":"options","options":[{"text":"隐藏","when":"false","steps":[]}]},
              {"kind":"options","options":[{"text":"可见","steps":[{"kind":"dialogue","speaker":"主角","text":"选择完成"}]}]}
            ]}
          ]},
          {"name":"sub","steps":[
            {"kind":"branch","cases":[{"when":"true","steps":[{"kind":"return"}]}]},
            {"kind":"dialogue","speaker":"主角","text":"不应执行"}
          ]}
        ]}
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
        var script = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"choice","prompt":{"speaker":"主角","text":"无路可走"},"blocks":[
            {"kind":"options","options":[{"text":"隐藏","when":"false","steps":[]}]}
          ]}
        ]}]}
        """);
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]));

        var exception = await Assert.ThrowsAsync<StoryRuntimeException>(() =>
            session.StoryService.ExecuteAsync("start"));
        Assert.Contains("no available options", exception.Message);
    }

    [Fact]
    public async Task Runtime_MixesChoiceBlocksAndEvaluatesOnlyVisitedConditionsOnce()
    {
        var script = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"choice","prompt":{"speaker":"主角","text":"选择"},"blocks":[
            {"kind":"options","options":[
              {"text":"隐藏","when":"chance(0)","steps":[]},
              {"text":"普通","steps":[]}
            ]},
            {"kind":"branch","cases":[
              {"when":"chance(1)","options":[{"text":"首个分支","when":"chance(1)","steps":[]}]},
              {"when":"chance(1)","options":[{"text":"未访问分支","when":"chance(1)","steps":[]}]}
            ],"fallback":[{"text":"回退","when":"chance(1)","steps":[]}]},
            {"kind":"branch","cases":[
              {"when":"chance(1)","options":[{"text":"独立分支","steps":[]}]}
            ],"fallback":null}
          ]}
        ]}]}
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

    [Fact]
    public void JsonParser_RejectsOldChoiceGroups()
    {
        const string json = """
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"choice","prompt":{"speaker":"主角","text":"旧结构"},"groups":[
            {"options":[{"text":"旧选项","steps":[]}]}
          ]}
        ]}]}
        """;

        var exception = Assert.Throws<StoryRuntimeException>(() => StoryScriptJson.Parse(json));
        Assert.Contains("old 'groups' shape", exception.Message);
    }

    [Theory]
    [InlineData("{\"kind\":\"options\",\"options\":[]}")]
    [InlineData("{\"kind\":\"branch\",\"cases\":[],\"fallback\":null}")]
    [InlineData("{\"kind\":\"branch\",\"cases\":[{\"when\":\"true\",\"options\":[{\"text\":\"x\",\"steps\":[]}]}]}")]
    [InlineData("{\"kind\":\"branch\",\"cases\":[{\"when\":\"true\",\"options\":[{\"text\":\"x\",\"steps\":[]}]}],\"fallback\":[]}")]
    public void JsonParser_RejectsMalformedChoiceBlocks(string block)
    {
        var json = $$"""
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"choice","prompt":{"speaker":"主角","text":"错误"},"blocks":[{{block}}]}
        ]}]}
        """;

        Assert.Throws<StoryRuntimeException>(() => StoryScriptJson.Parse(json));
    }

    [Theory]
    [InlineData(BattleOutcome.Win, false, true)]
    [InlineData(BattleOutcome.Lose, true, false)]
    public async Task Runtime_HandlesBattleOutcomesWithoutExplicitBranches(
        BattleOutcome outcome,
        bool expectedGameOver,
        bool expectedContinuation)
    {
        var script = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"start","steps":[
          {"kind":"battle","battleId":"test","outcomes":{}},
          {"kind":"dialogue","speaker":"主角","text":"继续"}
        ]}]}
        """);
        var host = new RecordingHost { BattleOutcome = outcome };
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]), host);

        var events = new List<StoryEvent>();
        await foreach (var storyEvent in session.StoryService.RunAsync("start"))
        {
            events.Add(storyEvent);
        }

        Assert.Equal(expectedGameOver, host.GameOverInvoked);
        Assert.Equal(expectedContinuation, host.DialogueTexts.Contains("继续"));
        Assert.Equal(expectedGameOver, events.OfType<StoryTerminatedEvent>().Count() == 1);
    }

    private sealed class RecordingHost : IRuntimeHost
    {
        public List<string> DialogueTexts { get; } = [];
        public List<int> OfferedOptionIndices { get; } = [];
        public int SelectedOptionIndex { get; init; }
        public BattleOutcome BattleOutcome { get; init; } = BattleOutcome.Win;
        public bool GameOverInvoked { get; private set; }
        public bool ContinueOnCommandFailure { get; init; }
        public List<(string Name, string Message)> CommandFailures { get; } = [];
        [StoryCommand("terminate_story")]
        public StoryCommandResult TerminateStory() => StoryCommandResult.Terminate;
        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) { DialogueTexts.Add(dialogue.Text); return ValueTask.CompletedTask; }
        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken)
        {
            OfferedOptionIndices.AddRange(choice.Options.Select(static option => option.Index));
            return ValueTask.FromResult(SelectedOptionIndex);
        }
        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) => ValueTask.FromResult(BattleOutcome);
        public ValueTask GameOverAsync(CancellationToken cancellationToken) { GameOverInvoked = true; return ValueTask.CompletedTask; }
        public ValueTask CommandFailedAsync(string commandName, string message, CancellationToken cancellationToken)
        {
            CommandFailures.Add((commandName, message));
            return ValueTask.CompletedTask;
        }
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
