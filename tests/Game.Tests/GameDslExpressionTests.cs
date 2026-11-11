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
        var script = Game.StoryDsl.StoryCompiler.Compile("""
            # x
            item_target = 'other'
            """);
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
}
