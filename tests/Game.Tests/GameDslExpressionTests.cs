using Game.Application;
using Game.Core.Abstractions;
using Game.Core.Model;

namespace Game.Tests;

public sealed class GameDslExpressionTests
{
    [Fact]
    public void CurrentTimeSlotUsesChineseEarthlyBranchName()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var expression = new ExpressionParser().ParseExpression("current_time_slot == '辰'");

        var result = new ExpressionEvaluator().Evaluate(
            expression,
            new GameExpressionEnvironment(session).Create());

        Assert.True(result.AsBoolean("test"));
    }

    [Fact]
    public void FriendCountCountsMembersWithoutFollowers()
    {
        var state = new GameState();
        state.Party.AddMember(TestContentFactory.CreateCharacterInstance(
            "hero", TestContentFactory.CreateCharacterDefinition("hero")));
        state.Party.AddMember(TestContentFactory.CreateCharacterInstance(
            "ally", TestContentFactory.CreateCharacterDefinition("ally")));
        state.Party.AddFollower(TestContentFactory.CreateCharacterInstance(
            "follower", TestContentFactory.CreateCharacterDefinition("follower")));
        var session = new GameSession(state, TestContentFactory.CreateRepository());
        var environment = new GameExpressionEnvironment(session).Create();
        var evaluator = new ExpressionEvaluator();

        Assert.True(evaluator.EvaluateBoolean(
            new ExpressionParser().ParseExpression("friend_count() >= 2"), environment, "test"));
        Assert.False(evaluator.EvaluateBoolean(
            new ExpressionParser().ParseExpression("friend_count() >= 3"), environment, "test"));
        Assert.True(evaluator.EvaluateBoolean(
            new ExpressionParser().ParseExpression("friendcount() >= 2"), environment, "test"));
    }

    [Fact]
    public void XmjhCalendarProfileAndCompletionValuesAreResolvable()
    {
        var state = new GameState();
        state.Clock.AdvanceDays(9);
        state.Story.MarkCompleted("循环剧情", state.Clock);
        state.Clock.AdvanceDays(4);
        state.Story.MarkCompleted("循环剧情", state.Clock);
        state.Clock.AdvanceDays(2);
        var profile = new GameProfile();
        profile.UnlockAchievement("称号一");
        profile.UnlockAchievement("称号二");
        profile.AddKills(12);
        var session = new GameSession(
            state,
            TestContentFactory.CreateRepository(),
            initialProfile: profile,
            timeProvider: new FixedTimeProvider(new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.FromHours(8))));
        var expression = new ExpressionParser().ParseExpression(
            "current_date == 10116 and system_date == 20260830 and achievement_count == 2 and kill_count == 12 " +
            "and story_completion_count('循环剧情') == 2 and story_elapsed_days('循环剧情') == 2");

        Assert.True(new ExpressionEvaluator().EvaluateBoolean(
            expression,
            new GameExpressionEnvironment(session).Create(),
            "test"));
    }

    [Fact]
    public void CharacterGenderReadsPartyOrContentDefinition()
    {
        var heroine = TestContentFactory.CreateCharacterDefinition("女侠", gender: CharacterGender.Female);
        var animal = TestContentFactory.CreateCharacterDefinition("灵兽", gender: CharacterGender.Animal);
        var state = new GameState();
        state.Party.AddMember(TestContentFactory.CreateCharacterInstance("女侠", heroine));
        var session = new GameSession(state, TestContentFactory.CreateRepository(characters: [heroine, animal]));
        var expression = new ExpressionParser().ParseExpression(
            "character_gender('女侠') == 'female' and character_gender('灵兽') == 'animal'");

        Assert.True(new ExpressionEvaluator().EvaluateBoolean(
            expression,
            new GameExpressionEnvironment(session).Create(),
            "test"));
    }

    [Fact]
    public void ChanceUsesInjectedRandomAndShortCircuitControlsConsumption()
    {
        var random = new RecordingRandom(.25);
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(), randomService: random);
        var environment = new GameExpressionEnvironment(session).Create();
        var parser = new ExpressionParser();
        var evaluator = new ExpressionEvaluator();

        Assert.False(evaluator.Evaluate(parser.ParseExpression("false && chance(1)"), environment).AsBoolean("test"));
        Assert.Equal(0, random.DoubleCalls);
        Assert.True(evaluator.Evaluate(parser.ParseExpression("chance(0.5)"), environment).AsBoolean("test"));
        Assert.Equal(1, random.DoubleCalls);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void ChanceRejectsOutOfRangeProbability(double probability)
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(), randomService: new RecordingRandom(.5));
        var expression = new ExpressionParser().ParseExpression($"chance({probability.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExpressionEvaluator().Evaluate(expression, new GameExpressionEnvironment(session).Create()));
    }

    [Fact]
    public void SkillLevelReturnsZeroWhenActiveCharacterDoesNotKnowSkill()
    {
        var state = new GameState();
        state.Party.AddMember(TestContentFactory.CreateCharacterInstance(
            "主角",
            TestContentFactory.CreateCharacterDefinition("主角")));
        var session = new GameSession(state, TestContentFactory.CreateRepository());
        var expression = new ExpressionParser().ParseExpression("skill_level('主角', '未学习武功')");

        var result = new ExpressionEvaluator().Evaluate(
            expression,
            new GameExpressionEnvironment(session).Create());

        Assert.Equal(0, result.AsNumber("test"));
    }

    [Fact]
    public void SkillLevelStillRejectsCharacterOutsideActiveParty()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var expression = new ExpressionParser().ParseExpression("skill_level('队外角色', '任意武功')");

        Assert.Throws<InvalidOperationException>(() => new ExpressionEvaluator().Evaluate(
            expression,
            new GameExpressionEnvironment(session).Create()));
    }

    [Fact]
    public void MapEventCompletedReadsExplicitMapLocationAndEventIds()
    {
        var state = new GameState();
        var session = new GameSession(state, TestContentFactory.CreateRepository());
        var expression = new ExpressionParser().ParseExpression(
            "map_event_completed('大地图', '黑木崖', '岳父')");
        var evaluator = new ExpressionEvaluator();
        var environment = new GameExpressionEnvironment(session).Create();

        Assert.False(evaluator.EvaluateBoolean(expression, environment, "test"));

        state.MapEventProgress.MarkCompleted("大地图", "黑木崖", "岳父");

        Assert.True(evaluator.EvaluateBoolean(expression, environment, "test"));
    }

    [Fact]
    public async Task ContextVariableCannotBeOverwrittenByAssignment()
    {
        const string json = """
        {"version":3,"segments":[{"name":"x","steps":[{"kind":"set","target":"item_target","value":"'other'"}]}]}
        """;
        var script = Game.Core.Story.StoryScriptJson.Parse(json);
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(storyScripts: [script]));
        var context = new StoryExecutionContext(new Dictionary<string, ExpressionValue> { ["item_target"] = ExpressionValue.FromString("hero") });
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.StoryService.ExecuteAsync("x", context));
    }

    private sealed class RecordingRandom(double nextDouble) : IRandomService
    {
        public int DoubleCalls { get; private set; }
        public double NextDouble() { DoubleCalls++; return nextDouble; }
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone { get; } = TimeZoneInfo.CreateCustomTimeZone(
            "test-cn",
            TimeSpan.FromHours(8),
            "test-cn",
            "test-cn");
    }
}
