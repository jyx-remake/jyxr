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
                    1,
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
    public async Task RunAsync_ResolvesCurrencyProjectionVariablesFromGameState()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    1,
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
        state.Currency.AddGold(3);
        state.Story.SetVariable("money", ExprValue.FromNumber(999));
        var host = new RecordingRuntimeHost();
        var session = new GameSession(state, repository, host);

        await foreach (var _ in session.StoryService.RunAsync("currency_projection"))
        {
        }

        var command = Assert.Single(host.CustomCommands);
        Assert.Equal("custom_cmd", command.Name);
        Assert.Equal(120, command.Args[0].AsNumber("money"));
        Assert.Equal(120, command.Args[1].AsNumber("silver"));
        Assert.Equal(3, command.Args[2].AsNumber("gold"));
        Assert.Equal(3, command.Args[3].AsNumber("yuanbao"));
    }

    [Fact]
    public async Task RunAsync_SetTimeKeyRegistersExpiringStoryTarget()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    1,
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

        await foreach (var _ in session.StoryService.RunAsync("start_timer"))
        {
        }

        var timeKey = Assert.Single(session.State.Story.TimeKeys.Values);
        Assert.Equal("rescue", timeKey.Key);
        Assert.Equal(3, timeKey.LimitDays);
        Assert.Equal("rescue_timeout", timeKey.TargetStoryId);
        Assert.Equal(1, timeKey.StartedAt.Year);
        Assert.Equal(1, timeKey.StartedAt.Month);
        Assert.Equal(1, timeKey.StartedAt.Day);
        Assert.Equal(TimeSlot.Chen, timeKey.StartedAt.TimeSlot);
        Assert.Equal(4, timeKey.DeadlineAt.Day);
        Assert.False(timeKey.Triggered);

        session.State.Clock.AdvanceDays(2);
        Assert.Empty(session.StoryTimeKeyExpirationService.CheckExpired());

        session.State.Clock.AdvanceDays(1);
        var expired = Assert.Single(session.StoryTimeKeyExpirationService.CheckExpired());
        Assert.Equal("rescue", expired.Key);
        Assert.Equal("rescue_timeout", expired.TargetStoryId);
        Assert.True(timeKey.Triggered);
        Assert.NotNull(timeKey.TriggeredAt);
        Assert.Empty(session.StoryTimeKeyExpirationService.CheckExpired());
    }

    [Fact]
    public async Task RunAsync_ClearTimeKeyCancelsExpiration()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    1,
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

        await foreach (var _ in session.StoryService.RunAsync("timer_flow"))
        {
        }

        Assert.Empty(session.State.Story.TimeKeys);
        session.State.Clock.AdvanceDays(1);
        Assert.Empty(session.StoryTimeKeyExpirationService.CheckExpired());
    }

    [Fact]
    public async Task RunAsync_AllowsJumpingToSegmentFromAnotherStoryScript()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(
                    1,
                    [
                        new Segment(
                            "start",
                            [
                                new JumpStep("external_segment"),
                            ]),
                    ]),
                new StoryScript(
                    1,
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

        await foreach (var _ in session.StoryService.RunAsync("start"))
        {
        }

        var command = Assert.Single(host.CustomCommands);
        Assert.Equal("custom_cmd", command.Name);
        Assert.Equal("ok", command.Args[0].AsString("custom_cmd"));
        Assert.True(session.State.Story.IsStoryCompleted("start"));
        Assert.True(session.State.Story.IsStoryCompleted("external_segment"));
    }

    [Fact]
    public async Task RunAsync_LearnCommandSupportsSkillsTalentsAndSpecialSkills()
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
                    1,
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

        await foreach (var _ in session.StoryService.RunAsync("learn_all"))
        {
        }

        Assert.Equal(5, hero.GetExternalSkillLevel("starter_sword"));
        Assert.Equal(4, hero.GetInternalSkillLevel("inner_breath"));
        Assert.True(hero.HasTalent("iron_body"));
        Assert.True(hero.HasEffectiveTalent("iron_body"));
        Assert.Equal(3, hero.GetStat(StatType.Gengu));
        Assert.Contains(hero.SpecialSkills, skill => skill.Definition.Id == "flash_step" && skill.IsActive);
    }

    [Fact]
    public async Task RunAsync_UpgradeCommandSupportsStatsAndSkillLevels()
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
                    1,
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

        await foreach (var _ in session.StoryService.RunAsync("upgrade_growth"))
        {
        }

        Assert.Equal(30, hero.GetBaseStat(StatType.MaxHp));
        Assert.Equal(5, hero.GetExternalSkillLevel("focus_strike"));
        Assert.Equal(4, hero.GetInternalSkillLevel("breath_control"));
        Assert.Equal(15, hero.GetStat(StatType.Gengu));
        Assert.Equal(7, hero.GetStat(StatType.Dingli));
    }

    [Fact]
    public async Task RunAsync_SupportsLegacyWeaponStatAndYuanbaoPredicates()
    {
        var repository = TestContentFactory.CreateRepository(
            characters:
            [
                TestContentFactory.CreateCharacterDefinition("hero"),
            ],
            storyScripts:
            [
                new StoryScript(
                    1,
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
        state.Currency.AddGold(1);
        var hero = TestContentFactory.CreateCharacterInstance("hero", repository.GetCharacter("hero"), state.EquipmentInstanceFactory);
        hero.AddBaseStat(StatType.Jianfa, 79);
        hero.AddBaseStat(StatType.Quanzhang, 80);
        hero.AddBaseStat(StatType.Daofa, 10);
        hero.AddBaseStat(StatType.Qimen, 81);
        var party = new Party();
        party.AddMember(hero);
        state.SetParty(party);
        var host = new RecordingRuntimeHost();
        var session = new GameSession(state, repository, host);

        await foreach (var _ in session.StoryService.RunAsync("legacy_predicates"))
        {
        }

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
    public async Task RunAsync_InterpolatesMaleAndFemalePlaceholdersInDialogueAndChoice()
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
                    1,
                    [
                        new Segment(
                            "interpolation",
                            [
                                new DialogueStep("$FEMALE$", "$MALE$，终于见到你了。"),
                                new ChoiceStep(
                                    new ChoicePrompt("旁白", "要和$FEMALE$一起行动吗？"),
                                    [
                                        new ChoiceOption("$MALE$，出发。", []),
                                        new ChoiceOption("再等等。", []),
                                    ]),
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

        await foreach (var _ in session.StoryService.RunAsync("interpolation"))
        {
        }

        var dialogue = Assert.Single(host.Dialogues);
        Assert.Equal("赵灵儿", dialogue.Speaker);
        Assert.Equal("张无忌，终于见到你了。", dialogue.Text);

        var choice = Assert.Single(host.Choices);
        Assert.Equal("旁白", choice.PromptSpeaker);
        Assert.Equal("要和赵灵儿一起行动吗？", choice.PromptText);
        Assert.Equal("张无忌，出发。", choice.Options[0].Text);
        Assert.Equal("再等等。", choice.Options[1].Text);
    }

    [Fact]
    public async Task RunAsync_InterpolatesPlaceholdersFromCharacterDefinitionsWhenRosterDoesNotContainCharacters()
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
                    1,
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

        await foreach (var _ in session.StoryService.RunAsync("interpolation_fallback"))
        {
        }

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
            CancellationToken cancellationToken) =>
            ValueTask.FromException<bool>(new InvalidOperationException($"Unknown predicate '{name}'."));

        public ValueTask ExecuteCommandAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken)
        {
            CustomCommands.Add((name, args));
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken)
        {
            Choices.Add(choice);
            return ValueTask.FromResult(0);
        }

        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
            ValueTask.FromResult(BattleOutcome.Win);
    }
}
