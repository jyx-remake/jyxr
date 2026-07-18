using Game.Application;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Tests;

public sealed class StoryServiceTests
{
    [Fact]
    public async Task RunAsync_ExecutesBuiltInStoryFlowAndUpdatesStoryState()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "story_intro",
                            [
                                new DialogueStep("旁白", "开场"),
                                new CommandStep("set_flag", [new LiteralExprNode(ExprValue.FromString("opened"))]),
                                new CommandStep("log", [new LiteralExprNode(ExprValue.FromString("踏入江湖"))]),
                                new JumpStep("story_second"),
                            ]),
                        new Segment(
                            "story_second",
                            [
                                new BranchStep(
                                    [
                                        new BranchCase(
                                            new PredicateExprNode(
                                                "should_finish",
                                                [new LiteralExprNode(ExprValue.FromString("story_intro"))]),
                                            [
                                                new CommandStep(
                                                    "custom_cmd",
                                                    [new VariableExprNode("external_number")]),
                                                new CommandStep(
                                                    "item",
                                                    [
                                                        new LiteralExprNode(ExprValue.FromString("quest_token")),
                                                        new LiteralExprNode(ExprValue.FromNumber(2)),
                                                    ]),
                                            ]),
                                    ],
                                    null),
                                new BranchStep(
                                    [
                                        new BranchCase(
                                            new PredicateExprNode(
                                                "have_item",
                                                [
                                                    new LiteralExprNode(ExprValue.FromString("quest_token")),
                                                    new LiteralExprNode(ExprValue.FromNumber(2)),
                                                ]),
                                            [
                                                new CommandStep(
                                                    "get_money",
                                                    [new LiteralExprNode(ExprValue.FromNumber(50))]),
                                            ]),
                                    ],
                                    null),
                                new CommandStep("map", [new LiteralExprNode(ExprValue.FromString("town"))]),
                            ]),
                    ]),
            ],
            items:
            [
                new NormalItemDefinition
                {
                    Id = "quest_token",
                    Name = "quest_token",
                    Type = ItemType.QuestItem,
                    ConsumeOnUse = false,
                },
            ],
            maps:
            [
                new MapDefinition
                {
                    Id = "town",
                    Name = "town",
                    Kind = MapKind.Small,
                },
            ]);

        var host = new RecordingRuntimeHost();
        var session = new GameSession(new GameState(), repository, host);

        var events = new List<StoryEvent>();
        await foreach (var storyEvent in session.StoryService.RunAsync("story_intro"))
        {
            events.Add(storyEvent);
        }

        Assert.Collection(
            events.OfType<SegmentStartedEvent>(),
            started => Assert.Equal("story_intro", started.SegmentId),
            started => Assert.Equal("story_second", started.SegmentId));
        Assert.Single(host.Dialogues);
        Assert.Equal(2, host.CustomCommands.Count);
        Assert.Equal("custom_cmd", host.CustomCommands[0].Name);
        Assert.Equal(7d, host.CustomCommands[0].Args[0].AsNumber("custom_cmd"));
        Assert.Equal("map", host.CustomCommands[1].Name);
        Assert.Equal("town", host.CustomCommands[1].Args[0].AsString("map"));
        Assert.True(session.State.Story.IsStoryCompleted("story_intro"));
        Assert.True(session.State.Story.IsStoryCompleted("story_second"));
        Assert.Equal("story_second", session.State.Story.LastStoryId);
        Assert.True(session.State.Story.TryGetVariable("opened", out var opened));
        Assert.True(opened.AsBoolean("opened"));
        var journalEntry = Assert.Single(session.State.Journal.Entries);
        Assert.Equal("踏入江湖", journalEntry.Text);
        Assert.True(session.State.Inventory.ContainsStack(repository.GetItem("quest_token"), 2));
        Assert.Equal(50, session.State.Currency.Silver);
    }

    [Fact]
    public async Task RunAsync_CommandResultJumpSwitchesToTargetSegment()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "start",
                            [
                                new CommandStep("jump_cmd", [new LiteralExprNode(ExprValue.FromString("target"))]),
                                new CommandStep("custom_cmd", [new LiteralExprNode(ExprValue.FromString("should_not_run"))]),
                            ]),
                        new Segment(
                            "target",
                            [
                                new CommandStep("custom_cmd", [new LiteralExprNode(ExprValue.FromString("landed"))]),
                            ]),
                    ]),
            ]);

        var host = new RecordingRuntimeHost();
        host.CommandJumps["jump_cmd"] = "target";
        var session = new GameSession(new GameState(), repository, host);

        var events = new List<StoryEvent>();
        await foreach (var storyEvent in session.StoryService.RunAsync("start"))
        {
            events.Add(storyEvent);
        }

        Assert.Collection(
            events.OfType<SegmentStartedEvent>(),
            started => Assert.Equal("start", started.SegmentId),
            started => Assert.Equal("target", started.SegmentId));
        Assert.Collection(
            host.CustomCommands,
            command => Assert.Equal("jump_cmd", command.Name),
            command =>
            {
                Assert.Equal("custom_cmd", command.Name);
                Assert.Equal("landed", command.Args[0].AsString("custom_cmd"));
            });
        var jump = Assert.Single(events.OfType<JumpEvent>());
        Assert.Equal("target", jump.Target);
    }

    [Fact]
    public async Task RunAsync_CallReturnsToCallerAndContinuesNextStep()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "start",
                            [
                                CreateCustomCommandStep("before_call"),
                                new CallStep("shared"),
                                CreateCustomCommandStep("after_call"),
                            ]),
                        new Segment(
                            "shared",
                            [
                                CreateCustomCommandStep("inside_call"),
                                new ReturnStep(),
                                CreateCustomCommandStep("after_return"),
                            ]),
                    ]),
            ]);

        var host = new RecordingRuntimeHost();
        var session = new GameSession(new GameState(), repository, host);

        await session.StoryService.ExecuteAsync("start");

        Assert.Equal(
            ["before_call", "inside_call", "after_call"],
            host.CustomCommands.Select(command => command.Args[0].AsString(command.Name)).ToArray());
        Assert.True(session.State.Story.IsStoryCompleted("start"));
        Assert.True(session.State.Story.IsStoryCompleted("shared"));
    }

    [Fact]
    public async Task RunAsync_NestedCallsReturnInStackOrder()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "start",
                            [
                                new CallStep("first"),
                                CreateCustomCommandStep("after_first"),
                            ]),
                        new Segment(
                            "first",
                            [
                                new CallStep("second"),
                                CreateCustomCommandStep("after_second"),
                                new ReturnStep(),
                            ]),
                        new Segment(
                            "second",
                            [
                                CreateCustomCommandStep("inside_second"),
                                new ReturnStep(),
                            ]),
                    ]),
            ]);

        var host = new RecordingRuntimeHost();
        var session = new GameSession(new GameState(), repository, host);

        await session.StoryService.ExecuteAsync("start");

        Assert.Equal(
            ["inside_second", "after_second", "after_first"],
            host.CustomCommands.Select(command => command.Args[0].AsString(command.Name)).ToArray());
    }

    [Fact]
    public async Task RunAsync_ReturnInsideNestedControlStructuresReturnsToCaller()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "start",
                            [
                                new CallStep("branch_return"),
                                CreateCustomCommandStep("after_branch"),
                                new CallStep("choice_return"),
                                CreateCustomCommandStep("after_choice"),
                                new CallStep("battle_return"),
                                CreateCustomCommandStep("after_battle"),
                            ]),
                        new Segment(
                            "branch_return",
                            [
                                new BranchStep(
                                    [
                                        new BranchCase(
                                            new LiteralExprNode(ExprValue.FromBoolean(true)),
                                            [new ReturnStep(), CreateCustomCommandStep("branch_unreachable")]),
                                    ],
                                    null),
                            ]),
                        new Segment(
                            "choice_return",
                            [
                                new ChoiceStep(
                                    new ChoicePrompt("旁白", "选"),
                                    [
                                        new ChoiceGroup(
                                            null,
                                            [
                                                new ChoiceOption("返回", [new ReturnStep(), CreateCustomCommandStep("choice_unreachable")]),
                                            ]),
                                    ]),
                            ]),
                        new Segment(
                            "battle_return",
                            [
                                new BattleStep(
                                    "battle_win",
                                    new Dictionary<BattleOutcome, IReadOnlyList<Step>>
                                    {
                                        [BattleOutcome.Win] = [new ReturnStep(), CreateCustomCommandStep("battle_unreachable")],
                                    }),
                            ]),
                    ]),
            ],
            battles:
            [
                new BattleDefinition
                {
                    Id = "battle_win",
                    Name = "battle_win",
                    MapId = "battle_map",
                },
            ]);

        var host = new RecordingRuntimeHost();
        var session = new GameSession(new GameState(), repository, host);

        await session.StoryService.ExecuteAsync("start");

        Assert.Equal(
            ["after_branch", "after_choice", "after_battle"],
            host.CustomCommands.Select(command => command.Args[0].AsString(command.Name)).ToArray());
        Assert.Single(host.Choices);
    }

    [Fact]
    public async Task RunAsync_FiltersConditionalChoiceGroupsAndPreservesSourceIndexes()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "conditional_choice",
                            [
                                new ChoiceStep(
                                    new ChoicePrompt("掌柜", "客官需要什么？"),
                                    [
                                        new ChoiceGroup(
                                            new PredicateExprNode("hidden_group", []),
                                            [new ChoiceOption("隐藏选项", [CreateCustomCommandStep("hidden")])]),
                                        new ChoiceGroup(
                                            new PredicateExprNode("visible_group", []),
                                            [
                                                new ChoiceOption("购买", [CreateCustomCommandStep("buy")]),
                                                new ChoiceOption("出售", [CreateCustomCommandStep("sell")]),
                                            ]),
                                    ]),
                            ]),
                    ]),
            ]);
        var host = new RecordingRuntimeHost
        {
            SelectedOptionIndex = 2,
        };
        host.PredicateResults["hidden_group"] = false;
        host.PredicateResults["visible_group"] = true;
        var session = new GameSession(new GameState(), repository, host);
        var events = new List<StoryEvent>();

        await foreach (var storyEvent in session.StoryService.RunAsync("conditional_choice"))
        {
            events.Add(storyEvent);
        }

        Assert.Equal(["hidden_group", "visible_group"], host.EvaluatedPredicates);
        var choice = Assert.Single(host.Choices);
        Assert.Equal([1, 2], choice.Options.Select(static option => option.Index).ToArray());
        Assert.Equal(["购买", "出售"], choice.Options.Select(static option => option.Text).ToArray());
        var offered = Assert.Single(events.OfType<ChoiceOfferedEvent>());
        Assert.Equal([1, 2], offered.Choice.Options.Select(static option => option.Index).ToArray());
        Assert.Equal("sell", Assert.Single(host.CustomCommands).Args[0].AsString("custom_cmd"));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsChoiceWithoutAvailableOptionsBeforeCallingHost()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "empty_choice",
                            [
                                new ChoiceStep(
                                    new ChoicePrompt("旁白", "无人可选"),
                                    [
                                        new ChoiceGroup(
                                            new PredicateExprNode("hidden_group", []),
                                            [new ChoiceOption("隐藏选项", [])]),
                                    ]),
                            ]),
                    ]),
            ]);
        var host = new RecordingRuntimeHost();
        host.PredicateResults["hidden_group"] = false;
        var session = new GameSession(new GameState(), repository, host);

        var exception = await Assert.ThrowsAsync<StoryRuntimeException>(
            () => session.StoryService.ExecuteAsync("empty_choice"));

        Assert.Contains("no available options", exception.Message, StringComparison.Ordinal);
        Assert.Empty(host.Choices);
    }

    [Fact]
    public async Task RunAsync_TopLevelReturnEndsStoryFlow()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "start",
                            [
                                CreateCustomCommandStep("before_return"),
                                new ReturnStep(),
                                CreateCustomCommandStep("after_return"),
                            ]),
                    ]),
            ]);

        var host = new RecordingRuntimeHost();
        var session = new GameSession(new GameState(), repository, host);

        await session.StoryService.ExecuteAsync("start");

        var command = Assert.Single(host.CustomCommands);
        Assert.Equal("before_return", command.Args[0].AsString(command.Name));
        Assert.True(session.State.Story.IsStoryCompleted("start"));
    }

    [Fact]
    public async Task RunAsync_JumpInsideCalledSegmentDoesNotReturnToCaller()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "start",
                            [
                                new CallStep("shared"),
                                CreateCustomCommandStep("after_call"),
                            ]),
                        new Segment(
                            "shared",
                            [
                                new JumpStep("target"),
                            ]),
                        new Segment(
                            "target",
                            [
                                CreateCustomCommandStep("landed"),
                            ]),
                    ]),
            ]);

        var host = new RecordingRuntimeHost();
        var session = new GameSession(new GameState(), repository, host);

        await session.StoryService.ExecuteAsync("start");

        var command = Assert.Single(host.CustomCommands);
        Assert.Equal("landed", command.Args[0].AsString(command.Name));
        Assert.True(session.State.Story.IsStoryCompleted("start"));
        Assert.True(session.State.Story.IsStoryCompleted("shared"));
        Assert.True(session.State.Story.IsStoryCompleted("target"));
    }

    [Fact]
    public async Task RunAsync_BattleWinWithoutWinOutcomeContinuesFollowingSteps()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "battle_story",
                            [
                                new BattleStep(
                                    "battle_win",
                                    new Dictionary<BattleOutcome, IReadOnlyList<Step>>
                                    {
                                        [BattleOutcome.Lose] =
                                        [
                                            new CommandStep(
                                                "custom_cmd",
                                                [new LiteralExprNode(ExprValue.FromString("lost"))]),
                                        ],
                                    }),
                                new CommandStep(
                                    "custom_cmd",
                                    [new LiteralExprNode(ExprValue.FromString("after_battle"))]),
                            ]),
                    ]),
            ]);

        var host = new RecordingRuntimeHost
        {
            BattleOutcome = BattleOutcome.Win,
        };
        var session = new GameSession(new GameState(), repository, host);

        var events = new List<StoryEvent>();
        await foreach (var storyEvent in session.StoryService.RunAsync("battle_story"))
        {
            events.Add(storyEvent);
        }

        var battleResolved = Assert.Single(events.OfType<BattleResolvedEvent>());
        Assert.Equal(BattleOutcome.Win, battleResolved.Outcome);
        var command = Assert.Single(host.CustomCommands);
        Assert.Equal("custom_cmd", command.Name);
        Assert.Equal("after_battle", command.Args[0].AsString("custom_cmd"));
        Assert.Contains(events, static storyEvent => storyEvent is SegmentCompletedEvent);
        Assert.True(session.State.Story.IsStoryCompleted("battle_story"));
        Assert.Equal("battle_story", session.State.Story.LastStoryId);
    }

    [Fact]
    public async Task RunAsync_BattleLoseWithoutLoseOutcomeTerminatesSegmentWithoutCompletingIt()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "battle_story",
                            [
                                new BattleStep(
                                    "battle_lose",
                                    new Dictionary<BattleOutcome, IReadOnlyList<Step>>
                                    {
                                        [BattleOutcome.Win] =
                                        [
                                            new CommandStep(
                                                "custom_cmd",
                                                [new LiteralExprNode(ExprValue.FromString("won"))]),
                                        ],
                                    }),
                                new CommandStep(
                                    "custom_cmd",
                                    [new LiteralExprNode(ExprValue.FromString("after_battle"))]),
                            ]),
                    ]),
            ]);

        var host = new RecordingRuntimeHost
        {
            BattleOutcome = BattleOutcome.Lose,
        };
        var session = new GameSession(new GameState(), repository, host);

        var events = new List<StoryEvent>();
        await foreach (var storyEvent in session.StoryService.RunAsync("battle_story"))
        {
            events.Add(storyEvent);
        }

        var battleResolved = Assert.Single(events.OfType<BattleResolvedEvent>());
        Assert.Equal(BattleOutcome.Lose, battleResolved.Outcome);
        Assert.DoesNotContain(events, static storyEvent => storyEvent is SegmentCompletedEvent);
        var command = Assert.Single(host.CustomCommands);
        Assert.Equal("gameover", command.Name);
        Assert.False(session.State.Story.IsStoryCompleted("battle_story"));
        Assert.Null(session.State.Story.LastStoryId);
    }

    [Fact]
    public async Task ExecuteAsync_ExecutesBuiltInStoryFlowAndUpdatesStoryState()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "story_intro",
                            [
                                new DialogueStep("旁白", "开场"),
                                new CommandStep("set_flag", [new LiteralExprNode(ExprValue.FromString("opened"))]),
                                new CommandStep("log", [new LiteralExprNode(ExprValue.FromString("踏入江湖"))]),
                                new JumpStep("story_second"),
                            ]),
                        new Segment(
                            "story_second",
                            [
                                new BranchStep(
                                    [
                                        new BranchCase(
                                            new PredicateExprNode(
                                                "should_finish",
                                                [new LiteralExprNode(ExprValue.FromString("story_intro"))]),
                                            [
                                                new CommandStep(
                                                    "custom_cmd",
                                                    [new VariableExprNode("external_number")]),
                                                new CommandStep(
                                                    "item",
                                                    [
                                                        new LiteralExprNode(ExprValue.FromString("quest_token")),
                                                        new LiteralExprNode(ExprValue.FromNumber(2)),
                                                    ]),
                                            ]),
                                    ],
                                    null),
                                new BranchStep(
                                    [
                                        new BranchCase(
                                            new PredicateExprNode(
                                                "have_item",
                                                [
                                                    new LiteralExprNode(ExprValue.FromString("quest_token")),
                                                    new LiteralExprNode(ExprValue.FromNumber(2)),
                                                ]),
                                            [
                                                new CommandStep(
                                                    "get_money",
                                                    [new LiteralExprNode(ExprValue.FromNumber(50))]),
                                            ]),
                                    ],
                                    null),
                                new CommandStep("map", [new LiteralExprNode(ExprValue.FromString("town"))]),
                            ]),
                    ]),
            ],
            items:
            [
                new NormalItemDefinition
                {
                    Id = "quest_token",
                    Name = "quest_token",
                    Type = ItemType.QuestItem,
                    ConsumeOnUse = false,
                },
            ],
            maps:
            [
                new MapDefinition
                {
                    Id = "town",
                    Name = "town",
                    Kind = MapKind.Small,
                },
            ]);

        var host = new RecordingRuntimeHost();
        var session = new GameSession(new GameState(), repository, host);

        await session.StoryService.ExecuteAsync("story_intro");

        Assert.Single(host.Dialogues);
        Assert.Equal(2, host.CustomCommands.Count);
        Assert.Equal("custom_cmd", host.CustomCommands[0].Name);
        Assert.Equal(7d, host.CustomCommands[0].Args[0].AsNumber("custom_cmd"));
        Assert.Equal("map", host.CustomCommands[1].Name);
        Assert.Equal("town", host.CustomCommands[1].Args[0].AsString("map"));
        Assert.True(session.State.Story.IsStoryCompleted("story_intro"));
        Assert.True(session.State.Story.IsStoryCompleted("story_second"));
        Assert.Equal("story_second", session.State.Story.LastStoryId);
        Assert.True(session.State.Story.TryGetVariable("opened", out var opened));
        Assert.True(opened.AsBoolean("opened"));
        var journalEntry = Assert.Single(session.State.Journal.Entries);
        Assert.Equal("踏入江湖", journalEntry.Text);
        Assert.True(session.State.Inventory.ContainsStack(repository.GetItem("quest_token"), 2));
        Assert.Equal(50, session.State.Currency.Silver);
    }

    [Fact]
    public async Task ExecuteAsync_ItemCommandTreatsNegativeQuantityAsRemoval()
    {
        var token = new NormalItemDefinition
        {
            Id = "quest_token",
            Name = "quest_token",
            Type = ItemType.QuestItem,
            ConsumeOnUse = false,
        };
        var repository = TestContentFactory.CreateRepository(
            items: [token],
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "item_delta",
                            [
                                new CommandStep(
                                    "item",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("quest_token")),
                                        new LiteralExprNode(ExprValue.FromNumber(3)),
                                    ]),
                                new CommandStep(
                                    "item",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("quest_token")),
                                        new LiteralExprNode(ExprValue.FromNumber(-1)),
                                    ]),
                            ]),
                    ]),
            ]);
        var session = new GameSession(new GameState(), repository, new RecordingRuntimeHost());

        await session.StoryService.ExecuteAsync("item_delta");

        Assert.True(session.State.Inventory.ContainsStack(token, 2));
        Assert.Equal(2, session.State.Inventory.GetStack(token).Quantity);
    }

    [Fact]
    public async Task ExecuteAsync_ResolvesCurrencyProjectionVariablesFromGameState()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "currency_projection",
                            [
                                new CommandStep(
                                    "custom_cmd",
                                    [
                                        new VariableExprNode("money"),
                                        new VariableExprNode("silver"),
                                        new VariableExprNode("gold"),
                                        new VariableExprNode("yuanbao"),
                                    ]),
                            ]),
                    ]),
        ]);
        var state = new GameState();
        state.Currency.AddSilver(120);
        state.Story.SetVariable("money", ExprValue.FromNumber(999));
        var profile = new GameProfile();
        profile.SetYuanbao(3);
        var host = new RecordingRuntimeHost();
        var session = new GameSession(state, repository, host, initialProfile: profile);

        await session.StoryService.ExecuteAsync("currency_projection");

        var command = Assert.Single(host.CustomCommands);
        Assert.Equal("custom_cmd", command.Name);
        Assert.Equal(120, command.Args[0].AsNumber("money"));
        Assert.Equal(120, command.Args[1].AsNumber("silver"));
        Assert.Equal(3, command.Args[2].AsNumber("gold"));
        Assert.Equal(3, command.Args[3].AsNumber("yuanbao"));
    }

    [Fact]
    public async Task ExecuteAsync_SetTimeKeyRegistersExpiringStoryTarget()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "start_timer",
                            [
                                new CommandStep(
                                    "set_time_key",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("rescue")),
                                        new LiteralExprNode(ExprValue.FromNumber(3)),
                                        new LiteralExprNode(ExprValue.FromString("rescue_timeout")),
                                    ]),
                            ]),
                        new Segment(
                            "rescue_timeout",
                            [
                                new CommandStep("custom_cmd", [new LiteralExprNode(ExprValue.FromString("timeout"))]),
                            ]),
                    ]),
            ]);
        var session = new GameSession(new GameState(), repository, new RecordingRuntimeHost());

        await session.StoryService.ExecuteAsync("start_timer");

        var timeKey = Assert.Single(session.State.Story.TimeKeys.Values);
        Assert.Equal("rescue", timeKey.Key);
        Assert.Equal(3, timeKey.LimitDays);
        Assert.Equal("rescue_timeout", timeKey.TargetStoryId);
        Assert.Equal(1, timeKey.StartedAt.Year);
        Assert.Equal(1, timeKey.StartedAt.Month);
        Assert.Equal(1, timeKey.StartedAt.Day);
        Assert.Equal(TimeSlot.Chen, timeKey.StartedAt.TimeSlot);
        Assert.Equal(4, timeKey.DeadlineAt.Day);

        session.State.Clock.AdvanceDays(3);
        Assert.Empty(session.StoryTimeKeyExpirationService.CheckExpired());

        session.State.Clock.AdvanceTimeSlots(1);
        var expired = Assert.Single(session.StoryTimeKeyExpirationService.CheckExpired());
        Assert.Equal("rescue", expired.Key);
        Assert.Equal("rescue_timeout", expired.TargetStoryId);
        Assert.Empty(session.State.Story.TimeKeys);
        Assert.Empty(session.StoryTimeKeyExpirationService.CheckExpired());
    }

    [Fact]
    public async Task ExecuteAsync_SetTimeKeyWithoutStoryOnlyClearsExpiredKey()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "start_timer",
                            [
                                new CommandStep(
                                    "set_time_key",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("cooldown")),
                                        new LiteralExprNode(ExprValue.FromNumber(1)),
                                    ]),
                            ]),
                    ]),
            ]);
        var session = new GameSession(new GameState(), repository, new RecordingRuntimeHost());

        await session.StoryService.ExecuteAsync("start_timer");

        var timeKey = Assert.Single(session.State.Story.TimeKeys.Values);
        Assert.Equal("cooldown", timeKey.Key);
        Assert.Equal(1, timeKey.LimitDays);
        Assert.Empty(timeKey.TargetStoryId);

        session.State.Clock.AdvanceDays(1);
        Assert.Empty(session.StoryTimeKeyExpirationService.CheckExpired());
        Assert.True(session.State.Story.HasTimeKey("cooldown"));

        session.State.Clock.AdvanceTimeSlots(1);
        Assert.Empty(session.StoryTimeKeyExpirationService.CheckExpired());
        Assert.False(session.State.Story.HasTimeKey("cooldown"));
    }

    [Fact]
    public async Task ExecuteAsync_ClearTimeKeyCancelsExpiration()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "timer_flow",
                            [
                                new CommandStep(
                                    "set_time_key",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("rescue")),
                                        new LiteralExprNode(ExprValue.FromNumber(1)),
                                        new LiteralExprNode(ExprValue.FromString("rescue_timeout")),
                                    ]),
                                new CommandStep(
                                    "clear_time_key",
                                    [new LiteralExprNode(ExprValue.FromString("rescue"))]),
                            ]),
                        new Segment("rescue_timeout", []),
                    ]),
            ]);
        var session = new GameSession(new GameState(), repository, new RecordingRuntimeHost());

        await session.StoryService.ExecuteAsync("timer_flow");

        Assert.Empty(session.State.Story.TimeKeys);
        session.State.Clock.AdvanceDays(1);
        Assert.Empty(session.StoryTimeKeyExpirationService.CheckExpired());
    }

    [Fact]
    public async Task ExecuteAsync_AllowsJumpingToSegmentFromAnotherStoryScript()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "start",
                            [
                                new JumpStep("external_segment"),
                            ]),
                    ]),
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "external_segment",
                            [
                                new CommandStep("custom_cmd", [new LiteralExprNode(ExprValue.FromString("ok"))]),
                            ]),
                    ]),
            ]);
        var host = new RecordingRuntimeHost();
        var session = new GameSession(new GameState(), repository, host);

        await session.StoryService.ExecuteAsync("start");

        var command = Assert.Single(host.CustomCommands);
        Assert.Equal("custom_cmd", command.Name);
        Assert.Equal("ok", command.Args[0].AsString("custom_cmd"));
        Assert.True(session.State.Story.IsStoryCompleted("start"));
        Assert.True(session.State.Story.IsStoryCompleted("external_segment"));
    }

    [Fact]
    public async Task ExecuteAsync_LearnCommandSupportsSkillsTalentsAndSpecialSkills()
    {
        var externalSkill = TestContentFactory.CreateExternalSkill("starter_sword");
        var internalSkill = TestContentFactory.CreateInternalSkill("inner_breath");
        var talent = new TalentDefinition
        {
            Id = "iron_body",
            Name = "Iron Body",
            Affixes = [new StatModifierAffix(StatType.Gengu, ModifierValue.Add(3))],
        };
        var specialSkill = new SpecialSkillDefinition(
            "flash_step",
            "Flash Step",
            "",
            SpecialSkillIntent.Support,
            "",
            0,
            new SkillCostDefinition(),
            new SkillTargetingDefinition(),
            "",
            "",
            null,
            []);
        var characterDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var repository = TestContentFactory.CreateRepository(
            characters: [characterDefinition],
            externalSkills: [externalSkill],
            internalSkills: [internalSkill],
            talents: [talent],
            specialSkills: [specialSkill],
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "learn_all",
                            [
                                new CommandStep(
                                    "learn",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("skill")),
                                        new LiteralExprNode(ExprValue.FromString("hero")),
                                        new LiteralExprNode(ExprValue.FromString("starter_sword")),
                                        new LiteralExprNode(ExprValue.FromNumber(5)),
                                    ]),
                                new CommandStep(
                                    "learn",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("skill")),
                                        new LiteralExprNode(ExprValue.FromString("hero")),
                                        new LiteralExprNode(ExprValue.FromString("inner_breath")),
                                        new LiteralExprNode(ExprValue.FromNumber(4)),
                                    ]),
                                new CommandStep(
                                    "learn",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("talent")),
                                        new LiteralExprNode(ExprValue.FromString("hero")),
                                        new LiteralExprNode(ExprValue.FromString("iron_body")),
                                    ]),
                                new CommandStep(
                                    "learn",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("skill")),
                                        new LiteralExprNode(ExprValue.FromString("hero")),
                                        new LiteralExprNode(ExprValue.FromString("flash_step")),
                                    ]),
                            ]),
                    ]),
            ]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", characterDefinition, state.EquipmentInstanceFactory);
        var party = new Party();
        party.AddMember(hero);
        state.SetParty(party);
        var session = new GameSession(state, repository, new RecordingRuntimeHost());

        await session.StoryService.ExecuteAsync("learn_all");

        Assert.Equal(5, hero.GetExternalSkillLevel("starter_sword"));
        Assert.Equal(4, hero.GetInternalSkillLevel("inner_breath"));
        Assert.True(hero.HasTalent("iron_body"));
        Assert.True(hero.HasEffectiveTalent("iron_body"));
        Assert.Equal(3, hero.GetStat(StatType.Gengu));
        Assert.Contains(hero.SpecialSkills, skill => skill.Definition.Id == "flash_step" && skill.IsActive);
    }

    [Fact]
    public async Task ExecuteAsync_UpgradeCommandSupportsStatsAndSkillLevels()
    {
        var externalAffix = new SkillAffixDefinition(
            new StatModifierAffix(StatType.Gengu, ModifierValue.Add(5)),
            MinimumLevel: 5);
        var externalSkill = TestContentFactory.CreateExternalSkill(
            "focus_strike",
            affixes: [externalAffix]);
        var internalAffix = new SkillAffixDefinition(
            new StatModifierAffix(StatType.Dingli, ModifierValue.Add(7)),
            MinimumLevel: 4);
        var internalSkill = TestContentFactory.CreateInternalSkill(
            "breath_control",
            affixes: [internalAffix]);
        var characterDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.MaxHp] = 20,
                [StatType.Gengu] = 10,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(externalSkill, Level: 3)]);
        var repository = TestContentFactory.CreateRepository(
            characters: [characterDefinition],
            externalSkills: [externalSkill],
            internalSkills: [internalSkill],
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "upgrade_growth",
                            [
                                new CommandStep(
                                    "upgrade",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("maxhp")),
                                        new LiteralExprNode(ExprValue.FromString("hero")),
                                        new LiteralExprNode(ExprValue.FromNumber(10)),
                                    ]),
                                new CommandStep(
                                    "upgrade",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("external")),
                                        new LiteralExprNode(ExprValue.FromString("hero")),
                                        new LiteralExprNode(ExprValue.FromString("focus_strike")),
                                        new LiteralExprNode(ExprValue.FromNumber(2)),
                                    ]),
                                new CommandStep(
                                    "upgrade",
                                    [
                                        new LiteralExprNode(ExprValue.FromString("skill")),
                                        new LiteralExprNode(ExprValue.FromString("hero")),
                                        new LiteralExprNode(ExprValue.FromString("breath_control")),
                                        new LiteralExprNode(ExprValue.FromNumber(4)),
                                    ]),
                            ]),
                    ]),
            ]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", characterDefinition, state.EquipmentInstanceFactory);
        var party = new Party();
        party.AddMember(hero);
        state.SetParty(party);
        var session = new GameSession(state, repository, new RecordingRuntimeHost());

        await session.StoryService.ExecuteAsync("upgrade_growth");

        Assert.Equal(30, hero.GetBaseStat(StatType.MaxHp));
        Assert.Equal(5, hero.GetExternalSkillLevel("focus_strike"));
        Assert.Equal(4, hero.GetInternalSkillLevel("breath_control"));
        Assert.Equal(15, hero.GetStat(StatType.Gengu));
        Assert.Equal(7, hero.GetStat(StatType.Dingli));
    }

    [Fact]
    public async Task ExecuteAsync_SupportsLegacyWeaponStatAndYuanbaoPredicates()
    {
        var repository = TestContentFactory.CreateRepository(
            characters:
            [
                TestContentFactory.CreateCharacterDefinition("hero"),
            ],
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "legacy_predicates",
                            [
                                CreatePredicateBranch(
                                    "have_yuanbao",
                                    [ExprValue.FromNumber(1)],
                                    "gold_enough",
                                    "gold_not_enough"),
                                CreatePredicateBranch(
                                    "have_yuanbao",
                                    [ExprValue.FromNumber(2)],
                                    "gold_two_enough",
                                    "gold_two_not_enough"),
                                CreatePredicateBranch(
                                    "jianfa_less_than",
                                    [ExprValue.FromString("hero"), ExprValue.FromNumber(80)],
                                    "jianfa_lt_80",
                                    "jianfa_gte_80"),
                                CreatePredicateBranch(
                                    "quanzhang_less_than",
                                    [ExprValue.FromString("hero"), ExprValue.FromNumber(80)],
                                    "quanzhang_lt_80",
                                    "quanzhang_gte_80"),
                                CreatePredicateBranch(
                                    "daofa_less_than",
                                    [ExprValue.FromString("hero"), ExprValue.FromNumber(80)],
                                    "daofa_lt_80",
                                    "daofa_gte_80"),
                                CreatePredicateBranch(
                                    "qimen_less_than",
                                    [ExprValue.FromString("hero"), ExprValue.FromNumber(80)],
                                    "qimen_lt_80",
                                    "qimen_gte_80"),
                            ]),
                    ]),
            ]);

        var state = new GameState();
        var profile = new GameProfile();
        profile.SetYuanbao(1);
        var hero = TestContentFactory.CreateCharacterInstance("hero", repository.GetCharacter("hero"), state.EquipmentInstanceFactory);
        hero.AddBaseStat(StatType.Jianfa, 79);
        hero.AddBaseStat(StatType.Quanzhang, 80);
        hero.AddBaseStat(StatType.Daofa, 10);
        hero.AddBaseStat(StatType.Qimen, 81);
        var party = new Party();
        party.AddMember(hero);
        state.SetParty(party);
        var host = new RecordingRuntimeHost();
        var session = new GameSession(state, repository, host, initialProfile: profile);

        await session.StoryService.ExecuteAsync("legacy_predicates");

        Assert.Equal(
            [
                "custom_cmd:gold_enough",
                "custom_cmd:gold_two_not_enough",
                "custom_cmd:jianfa_lt_80",
                "custom_cmd:quanzhang_gte_80",
                "custom_cmd:daofa_lt_80",
                "custom_cmd:qimen_gte_80",
            ],
            host.CustomCommands.Select(command => $"{command.Name}:{command.Args[0].AsString(command.Name)}").ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_InterpolatesMaleAndFemalePlaceholdersInDialogueAndChoice()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(Party.HeroCharacterId);
        var femaleDefinition = TestContentFactory.CreateCharacterDefinition("女主");
        var repository = TestContentFactory.CreateRepository(
            characters:
            [
                heroDefinition,
                femaleDefinition,
            ],
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "interpolation",
                            [
                                new DialogueStep("$FEMALE$", "$MALE$，终于见到你了。"),
                                new ChoiceStep(
                                    new ChoicePrompt("旁白", "要和$FEMALE$一起行动吗？"),
                                    [
                                        new ChoiceGroup(
                                            null,
                                            [
                                                new ChoiceOption("$MALE$，出发。", []),
                                                new ChoiceOption("再等等。", []),
                                            ]),
                                    ],
                                    ChoiceStyle.Bold),
                            ]),
                    ]),
            ]);

        var state = new GameState();
        var party = new Party();
        var hero = TestContentFactory.CreateCharacterInstance(Party.HeroCharacterId, heroDefinition, state.EquipmentInstanceFactory);
        hero.Name = "张无忌";
        party.AddMember(hero);

        var female = TestContentFactory.CreateCharacterInstance("女主", femaleDefinition, state.EquipmentInstanceFactory);
        female.Name = "赵灵儿";
        party.AddReserve(female);
        state.SetParty(party);

        var host = new RecordingRuntimeHost();
        var session = new GameSession(state, repository, host);

        await session.StoryService.ExecuteAsync("interpolation");

        var dialogue = Assert.Single(host.Dialogues);
        Assert.Equal("赵灵儿", dialogue.Speaker);
        Assert.Equal("张无忌，终于见到你了。", dialogue.Text);

        var choice = Assert.Single(host.Choices);
        Assert.Equal("旁白", choice.PromptSpeaker);
        Assert.Equal("要和赵灵儿一起行动吗？", choice.PromptText);
        Assert.Equal("张无忌，出发。", choice.Options[0].Text);
        Assert.Equal("再等等。", choice.Options[1].Text);
        Assert.Equal(ChoiceStyle.Bold, choice.Style);
    }

    [Fact]
    public async Task ExecuteAsync_InterpolatesPlaceholdersFromCharacterDefinitionsWhenRosterDoesNotContainCharacters()
    {
        var repository = TestContentFactory.CreateRepository(
            characters:
            [
                TestContentFactory.CreateCharacterDefinition(Party.HeroCharacterId),
                TestContentFactory.CreateCharacterDefinition("女主"),
            ],
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [
                        new Segment(
                            "interpolation_fallback",
                            [
                                new DialogueStep("旁白", "$MALE$与$FEMALE$初次相遇。"),
                            ]),
                    ]),
            ]);

        var host = new RecordingRuntimeHost();
        var session = new GameSession(new GameState(), repository, host);

        await session.StoryService.ExecuteAsync("interpolation_fallback");

        var dialogue = Assert.Single(host.Dialogues);
        Assert.Equal("主角与女主初次相遇。", dialogue.Text);
    }

    private static BranchStep CreatePredicateBranch(
        string predicateName,
        IReadOnlyList<ExprValue> args,
        string whenTrue,
        string whenFalse)
    {
        return new BranchStep(
            [
                new BranchCase(
                    new PredicateExprNode(
                        predicateName,
                        args.Select(static arg => new LiteralExprNode(arg)).ToArray()),
                    [CreateCustomCommandStep(whenTrue)]),
            ],
            [CreateCustomCommandStep(whenFalse)]);
    }

    private static CommandStep CreateCustomCommandStep(string value) =>
        new(
            "custom_cmd",
            [new LiteralExprNode(ExprValue.FromString(value))]);

    private sealed class RecordingRuntimeHost : IRuntimeHost
    {
        public List<DialogueContext> Dialogues { get; } = [];
        public List<ChoiceContext> Choices { get; } = [];

        public List<(string Name, IReadOnlyList<ExprValue> Args)> CustomCommands { get; } = [];

        public Dictionary<string, bool> PredicateResults { get; } = new(StringComparer.Ordinal);

        public List<string> EvaluatedPredicates { get; } = [];

        public Dictionary<string, string> CommandJumps { get; } = new(StringComparer.Ordinal);

        public BattleOutcome BattleOutcome { get; init; } = BattleOutcome.Win;

        public int SelectedOptionIndex { get; init; }

        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken)
        {
            Dialogues.Add(dialogue);
            return ValueTask.CompletedTask;
        }

        public ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken) =>
            name switch
            {
                "external_number" => ValueTask.FromResult(ExprValue.FromNumber(7)),
                _ => ValueTask.FromException<ExprValue>(new InvalidOperationException($"Unknown variable '{name}'.")),
            };

        public ValueTask<bool> EvaluatePredicateAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken)
        {
            EvaluatedPredicates.Add(name);
            return PredicateResults.TryGetValue(name, out var result)
                ? ValueTask.FromResult(result)
                : ValueTask.FromException<bool>(new InvalidOperationException($"Unknown predicate '{name}'."));
        }

        public ValueTask<StoryCommandResult> ExecuteCommandAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken)
        {
            CustomCommands.Add((name, args));
            return CommandJumps.TryGetValue(name, out var target)
                ? ValueTask.FromResult(StoryCommandResult.Jump(target))
                : ValueTask.FromResult(StoryCommandResult.None);
        }

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken)
        {
            Choices.Add(choice);
            return ValueTask.FromResult(SelectedOptionIndex);
        }

        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
            ValueTask.FromResult(BattleOutcome);
    }
}
