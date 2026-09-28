using Game.Application;
using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Tests;

public sealed class ExpressionCommandEventTests
{
    [Fact]
    public async Task CurrencyAndAdventureCommandsPublishEvents()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var currency = 0;
        var adventure = 0;
        using var a = session.Events.Subscribe<CurrencyChangedEvent>(_ => currency++);
        using var b = session.Events.Subscribe<AdventureStateChangedEvent>(_ => adventure++);

        await session.StoryService.CommandDispatcher.ExecuteCommandAsync("change_silver", [ExpressionValue.FromNumber(20)]);
        await session.StoryService.CommandDispatcher.ExecuteCommandAsync("change_morality", [ExpressionValue.FromNumber(5)]);

        Assert.Equal(1, currency);
        Assert.Equal(1, adventure);
    }

    [Fact]
    public async Task SetRound_RecordsOnlyHigherReachedRound()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var profileChanges = 0;
        using var subscription = session.Events.Subscribe<ProfileChangedEvent>(_ => profileChanges++);

        await session.StoryService.CommandLine.ExecuteAsync("set_round 4");
        await session.StoryService.CommandLine.ExecuteAsync("set_round 2");

        Assert.Equal(2, session.State.Adventure.Round);
        Assert.Equal(4, session.Profile.HighestRound);
        Assert.Equal(1, profileChanges);
    }

    [Fact]
    public async Task FlagCommandsPublishStoryStateEvents()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var count = 0;
        using var subscription = session.Events.Subscribe<StoryStateChangedEvent>(_ => count++);
        var dispatcher = session.StoryService.CommandDispatcher;

        await dispatcher.ExecuteCommandAsync("set_flag", [ExpressionValue.FromString("flag")]);
        await dispatcher.ExecuteCommandAsync("clear_flag", [ExpressionValue.FromString("flag")]);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task FlagSugarSafelyProbesStrictBooleanStoryVariables()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var dispatcher = session.StoryService.CommandDispatcher;
        var evaluator = new ExpressionEvaluator();
        var parser = new ExpressionParser();
        var environment = new GameExpressionEnvironment(session).Create();

        Assert.False(evaluator.Evaluate(parser.ParseExpression("has_flag('met_heroine')"), environment).AsBoolean("test"));

        await dispatcher.ExecuteCallAsync(parser.ParseCall("set_flag('met_heroine')"));

        Assert.True(evaluator.Evaluate(parser.ParseExpression("has_flag('met_heroine')"), environment).AsBoolean("test"));
        Assert.True(session.State.Story.TryGetVariable("met_heroine", out var flag));
        Assert.True(flag.AsBoolean("test"));

        await dispatcher.ExecuteCallAsync(parser.ParseCall("clear_flag('met_heroine')"));
        Assert.False(evaluator.Evaluate(parser.ParseExpression("has_flag('met_heroine')"), environment).AsBoolean("test"));
        await dispatcher.ExecuteCallAsync(parser.ParseCall("clear_flag('met_heroine')"));

        session.State.Story.SetVariable("counter", ExpressionValue.FromNumber(1));
        Assert.Throws<ExpressionEvaluationException>(() =>
            evaluator.Evaluate(parser.ParseExpression("has_flag('counter')"), environment));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.ExecuteCallAsync(parser.ParseCall("set_flag('counter')")));
    }

    [Fact]
    public async Task MissingVariableFlagAndTimeKeyClearWithWarningsWithoutStoppingExecution()
    {
        var logger = new CollectingDiagnosticLogger();
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(), logger);
        var storyChanges = 0;
        using var subscription = session.Events.Subscribe<StoryStateChangedEvent>(_ => storyChanges++);
        var dispatcher = session.StoryService.CommandDispatcher;
        var parser = new ExpressionParser();

        await dispatcher.ExecuteCallAsync(parser.ParseCall("clear_flag('missing_flag')"));
        await dispatcher.ExecuteCallAsync(parser.ParseCall("clear_time_key('missing_key')"));
        await dispatcher.ExecuteCallAsync(parser.ParseCall("set_flag('continued')"));

        Assert.Equal(1, storyChanges);
        Assert.True(session.State.Story.TryGetVariable("continued", out var continued));
        Assert.True(continued.AsBoolean("test"));
        Assert.Collection(
            logger.Entries.Where(entry => entry.Level == DiagnosticLogLevel.Warning),
            entry => Assert.Contains("missing_flag", entry.Message, StringComparison.Ordinal),
            entry => Assert.Contains("missing_key", entry.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SetTimeKeySupportsOptionalAndValidatedStoryTargets()
    {
        var script = StoryScriptJson.Parse("""
        {"version":3,"segments":[{"name":"timeout_story","steps":[]}]}
        """);
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(storyScripts: [script]));
        var dispatcher = session.StoryService.CommandDispatcher;
        var parser = new ExpressionParser();

        await dispatcher.ExecuteCallAsync(parser.ParseCall("set_time_key('plain', 2)"));
        await dispatcher.ExecuteCallAsync(parser.ParseCall("set_time_key('targeted', 3, 'timeout_story')"));

        Assert.Equal(string.Empty, session.State.Story.TimeKeys["plain"].TargetStoryId);
        Assert.Equal("timeout_story", session.State.Story.TimeKeys["targeted"].TargetStoryId);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await dispatcher.ExecuteCallAsync(parser.ParseCall("set_time_key('invalid', 1, 'missing_story')")));
        Assert.False(session.State.Story.HasTimeKey("invalid"));
    }

    [Fact]
    public void QueriesWarnForUnknownItemsAndTreatFollowersAsActiveCharacters()
    {
        var logger = new CollectingDiagnosticLogger();
        var skill = TestContentFactory.CreateExternalSkill("follower_skill");
        var definition = TestContentFactory.CreateCharacterDefinition(
            "follower",
            stats: new Dictionary<StatType, int> { [StatType.Bili] = 12 },
            externalSkills: [new InitialExternalSkillEntryDefinition(skill, 4)],
            level: 7);
        var follower = TestContentFactory.CreateCharacterInstance("follower", definition);
        var state = new GameState();
        state.Party.AddFollower(follower);
        var session = new GameSession(
            state,
            TestContentFactory.CreateRepository(characters: [definition], externalSkills: [skill]),
            logger);
        var evaluator = new ExpressionEvaluator();
        var parser = new ExpressionParser();
        var environment = new GameExpressionEnvironment(session).Create();

        Assert.Equal(0, evaluator.Evaluate(parser.ParseExpression("item_count('unknown')"), environment).AsInt32("test"));
        var warning = Assert.Single(logger.Entries, entry => entry.Level == DiagnosticLogLevel.Warning);
        Assert.Contains("unknown", warning.Message, StringComparison.Ordinal);
        Assert.True(evaluator.Evaluate(parser.ParseExpression("in_team('follower')"), environment).AsBoolean("test"));
        Assert.Equal(7, evaluator.Evaluate(parser.ParseExpression("character_level('follower')"), environment).AsInt32("test"));
        Assert.Equal(12, evaluator.Evaluate(parser.ParseExpression("character_stat('follower', 'bili')"), environment).AsInt32("test"));
        Assert.Equal(4, evaluator.Evaluate(parser.ParseExpression("skill_level('follower', 'follower_skill')"), environment).AsInt32("test"));
    }

    [Fact]
    public async Task UnlockAchievementAndNickRequireNickResourceGroup()
    {
        var valid = new ResourceDefinition { Id = "nick.hero", Group = "nick" };
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(resources: [valid]));

        await session.StoryService.CommandDispatcher.ExecuteCallAsync(
            new ExpressionParser().ParseCall("nick('hero')"));
        Assert.True(session.Profile.IsAchievementUnlocked("hero"));

        var invalid = new ResourceDefinition { Id = "nick.wrong_group", Group = "portrait" };
        var invalidSession = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(resources: [invalid]));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await invalidSession.StoryService.CommandDispatcher.ExecuteCallAsync(
                new ExpressionParser().ParseCall("unlock_achievement('wrong_group')")));
        Assert.False(invalidSession.Profile.IsAchievementUnlocked("wrong_group"));
    }

    [Fact]
    public async Task ChangeItemUsesSignedDeltaAndRemoveItemRequiresPositiveQuantity()
    {
        var item = new NormalItemDefinition { Id = "pill", Name = "pill", Type = ItemType.Utility, ConsumeOnUse = false };
        var host = new RecordingEffectHost();
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(items: [item]), host);
        var acquiredItems = new List<ItemAcquiredEvent>();
        using var acquisitionSubscription = session.Events.Subscribe<ItemAcquiredEvent>(acquiredItems.Add);
        await session.StoryService.CommandDispatcher.ExecuteCommandAsync("change_item", [ExpressionValue.FromString("pill"), ExpressionValue.FromNumber(3)]);
        Assert.Equal(new ItemAcquiredEvent("pill", "pill", 3), Assert.Single(acquiredItems));
        Assert.Empty(host.Effects);
        await session.StoryService.CommandDispatcher.ExecuteCommandAsync("item", [ExpressionValue.FromString("pill"), ExpressionValue.FromNumber(-1)]);
        Assert.True(session.State.Inventory.ContainsStack(item, 2));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await session.StoryService.CommandDispatcher.ExecuteCommandAsync("remove_item", [ExpressionValue.FromString("pill"), ExpressionValue.FromNumber(-1)]));
        var parser = new ExpressionParser();
        await session.StoryService.CommandDispatcher.ExecuteCallAsync(parser.ParseCall("change_item('pill', 0)"));
        Assert.Equal(new[] { "音效.装备" }, host.Effects);
        await session.StoryService.CommandDispatcher.ExecuteCallAsync(parser.ParseCall("cost_item('pill')"));
        await session.StoryService.CommandDispatcher.ExecuteCallAsync(parser.ParseCall("item_random(['pill'])"));
        Assert.Equal(new[] { 3, 1 }, acquiredItems.Select(itemEvent => itemEvent.Quantity));
        Assert.Equal(new[] { "音效.装备", "音效.装备" }, host.Effects);
    }

    [Fact]
    public async Task MaxLevelSupportsOnceKeyDefaultLevelAndApprovedAlias()
    {
        var skill = TestContentFactory.CreateExternalSkill("starter_sword");
        var host = new RecordingEffectHost();
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(externalSkills: [skill]), host);
        var profileChanges = 0;
        var toasts = new List<ToastRequestedEvent>();
        using var profileSubscription = session.Events.Subscribe<ProfileChangedEvent>(_ => profileChanges++);
        using var toastSubscription = session.Events.Subscribe<ToastRequestedEvent>(toasts.Add);
        var dispatcher = session.StoryService.CommandDispatcher;
        var parser = new ExpressionParser();

        await dispatcher.ExecuteCallAsync(parser.ParseCall(
            "maxlevel('starter_sword', 2, 'reward.starter_sword')"));
        await dispatcher.ExecuteCallAsync(parser.ParseCall(
            "max_skill_level('starter_sword', 2, 'reward.starter_sword')"));

        Assert.Equal(2, session.Profile.GetSkillMaxLevelBonus("starter_sword"));
        Assert.Contains("reward.starter_sword", session.Profile.ConsumedSkillMaxLevelKeys);
        Assert.Equal(1, profileChanges);
        var toast = Assert.Single(toasts);
        Assert.Equal("武学精通【starter_sword】+ 2", toast.Message);
        Assert.Equal(ToastTone.Important, toast.Tone);
        Assert.Equal("音效.升级", toast.SoundEffectId);
        Assert.Empty(host.Effects);
    }

    [Fact]
    public async Task MaxLevelDefaultIncreaseIsIndependentOfRoundBonus()
    {
        var skill = TestContentFactory.CreateInternalSkill("starter_internal");
        var host = new RecordingEffectHost();
        var state = new GameState();
        state.Adventure.SetRound(5);
        var session = new GameSession(
            state,
            TestContentFactory.CreateRepository(internalSkills: [skill]), host,
            config: new GameConfig { RoundsPerMaxSkillLevelIncrease = 2 });

        await session.StoryService.CommandDispatcher.ExecuteCallAsync(
            new ExpressionParser().ParseCall("maxlevel('starter_internal')"));

        Assert.Equal(1, session.Profile.GetSkillMaxLevelBonus("starter_internal"));
        Assert.Equal(13, session.SkillMaxLevelPolicy.GetMaxLevel(skill));
        Assert.Empty(host.Effects);
    }

    [Theory]
    [InlineData("remove_item('item', 0)")]
    [InlineData("add_random_item([], 0)")]
    [InlineData("advance_days(0)")]
    [InlineData("set_round(0)")]
    [InlineData("set_time_key('key', 0, 'story')")]
    [InlineData("scale_stats('hero', -0.01)")]
    [InlineData("scale_stats('hero', 1.01)")]
    [InlineData("grant_points('hero', 0)")]
    [InlineData("grant_exp('hero', 0)")]
    [InlineData("level_up('hero', 0)")]
    [InlineData("upgrade_external('hero', 'skill', 0)")]
    [InlineData("upgrade_internal('hero', 'skill', 0)")]
    [InlineData("upgrade_skill('hero', 'skill', 0)")]
    [InlineData("maxlevel('skill', 0)")]
    [InlineData("learn_external('hero', 'skill', 0)")]
    [InlineData("learn_internal('hero', 'skill', 0)")]
    [InlineData("learn('hero', 'skill', 0)")]
    public async Task CommandsRejectValuesOutsideDocumentedRanges(string source)
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await session.StoryService.CommandDispatcher.ExecuteCallAsync(
                new ExpressionParser().ParseCall(source, "range test")));
    }

    [Fact]
    public async Task MaxLevelRejectsUnknownSkillBeforeChangingProfile()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await session.StoryService.CommandDispatcher.ExecuteCallAsync(
                new ExpressionParser().ParseCall("maxlevel('missing')")));
        Assert.Empty(session.Profile.SkillMaxLevelBonuses);
    }

    [Theory]
    [InlineData("join_random")]
    public async Task RandomJoinUsesInjectedRandomSource(string commandName)
    {
        var first = TestContentFactory.CreateCharacterDefinition("first");
        var second = TestContentFactory.CreateCharacterDefinition("second");
        var random = new SelectingRandom(1);
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(characters: [first, second]),
            randomService: random);
        var call = new ExpressionParser().ParseCall($"{commandName}(['first', 'second'])", "random join test");

        await session.StoryService.CommandDispatcher.ExecuteCallAsync(call);

        Assert.True(session.State.Party.ContainsMember("second"));
        Assert.Equal((0, 2), random.LastRange);
    }

    [Fact]
    public async Task RandomJoinValidatesEveryCandidateBeforeConsumingRandomness()
    {
        var first = TestContentFactory.CreateCharacterDefinition("first");
        var random = new SelectingRandom(0);
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(characters: [first]),
            randomService: random);
        var call = new ExpressionParser().ParseCall("join_random(['first', 'missing'])", "random join test");

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await session.StoryService.CommandDispatcher.ExecuteCallAsync(call));

        Assert.Null(random.LastRange);
        Assert.Empty(session.State.Party.Members);
    }

    [Fact]
    public async Task RandomJoinRejectsEmptyCandidateList()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var call = new ExpressionParser().ParseCall("join_random([])", "random join test");

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await session.StoryService.CommandDispatcher.ExecuteCallAsync(call));
    }

    [Theory]
    [InlineData("join", false)]
    [InlineData("follow", true)]
    public async Task PartyEntryCommandsPreserveOptionalDefinitionId(string commandName, bool follower)
    {
        var identityDefinition = TestContentFactory.CreateCharacterDefinition("identity");
        var templateDefinition = TestContentFactory.CreateCharacterDefinition("template");
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(characters: [identityDefinition, templateDefinition]));

        await session.StoryService.CommandDispatcher.ExecuteCallAsync(
            new ExpressionParser().ParseCall($"{commandName}('identity', 'template')", "party entry test"));

        var character = follower
            ? session.State.Party.Followers.Single(candidate => candidate.Id == "identity")
            : session.State.Party.GetMember("identity");
        Assert.Equal("template", character.Definition.Id);
    }

    [Theory]
    [InlineData("join", false)]
    [InlineData("follow", true)]
    public async Task PartyEntryCommandsDefaultDefinitionIdToCharacterId(string commandName, bool follower)
    {
        var definition = TestContentFactory.CreateCharacterDefinition("identity");
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(characters: [definition]));

        await session.StoryService.CommandDispatcher.ExecuteCallAsync(
            new ExpressionParser().ParseCall($"{commandName}('identity')", "party entry test"));

        var character = follower
            ? session.State.Party.Followers.Single(candidate => candidate.Id == "identity")
            : session.State.Party.GetMember("identity");
        Assert.Equal("identity", character.Definition.Id);
    }

    private sealed class RecordingEffectHost : IRuntimeHost
    {
        public List<string> Effects { get; } = [];

        public ValueTask PlayEffectAsync(string effectId, CancellationToken cancellationToken)
        {
            Effects.Add(effectId);
            return ValueTask.CompletedTask;
        }

        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class SelectingRandom(int selectedIndex) : IRandomService
    {
        public (int Minimum, int Maximum)? LastRange { get; private set; }

        public double NextDouble() => 0d;

        public int Next(int minInclusive, int maxExclusive)
        {
            LastRange = (minInclusive, maxExclusive);
            return selectedIndex;
        }
    }

    private sealed class CollectingDiagnosticLogger : IDiagnosticLogger
    {
        private readonly List<(DiagnosticLogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(DiagnosticLogLevel Level, string Message)> Entries => _entries;

        public void Log(DiagnosticLogLevel level, string message, Exception? exception = null) =>
            _entries.Add((level, message));
    }
}
