using System.Text.Json;
using Game.Application;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Serialization;
using Game.Core.Story;

namespace Game.Tests;

public sealed class FavorabilityStoryTests
{
    [Fact]
    public async Task ChangeFavorabilityCommand_RequiresExplicitTarget()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var dispatcher = new StoryCommandDispatcher(session, new ThrowingRuntimeHost());

        await dispatcher.ExecuteCommandAsync(
            "change_favorability",
            [ExpressionValue.FromString("李文秀"), ExpressionValue.FromNumber(5)],
            default);

        Assert.Equal(50, session.State.Adventure.GetFavorability());
        Assert.Equal(55, session.State.Adventure.GetFavorability("李文秀"));
        Assert.Equal(50, session.State.Adventure.GetFavorability("长安一梦阿玉"));
    }

    [Fact]
    public async Task HaoganCommandAlias_ChangesTargetedFavorability()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());

        await session.StoryService.CommandLine.ExecuteAsync("haogan '李文秀' 5");

        Assert.Equal(55, session.State.Adventure.GetFavorability("李文秀"));
    }

    [Fact]
    public void ChangeFavorability_ClampsAtZeroLikeLegacyHaogan()
    {
        // Legacy addHaogan floors the 50-base store at zero; XMJH reset flows
        // rely on large negative deltas (for example 采药#-100) landing on 0.
        var adventure = new AdventureState();

        adventure.ChangeFavorability("采药", -100);
        Assert.Equal(0, adventure.GetFavorability("采药"));

        adventure.ChangeFavorability("采药", -50);
        Assert.Equal(0, adventure.GetFavorability("采药"));

        adventure.ChangeFavorability("采药", 3);
        Assert.Equal(3, adventure.GetFavorability("采药"));
    }

    [Fact]
    public void FavorabilityFunction_ReturnsTargetedFavorability()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        session.State.Adventure.ChangeFavorability(10);
        session.State.Adventure.ChangeFavorability("李文秀", 5);
        var evaluator = new ExpressionEvaluator();
        var environment = new GameExpressionEnvironment(session).Create();

        Assert.True(evaluator.Evaluate(new ExpressionParser().ParseExpression("favorability('李文秀') == 55"), environment).AsBoolean("test"));
        Assert.True(evaluator.Evaluate(new ExpressionParser().ParseExpression("favorability('未记录角色') == 50"), environment).AsBoolean("test"));
        Assert.True(evaluator.Evaluate(new ExpressionParser().ParseExpression("favorability() == 60"), environment).AsBoolean("test"));
        Assert.True(evaluator.Evaluate(new ExpressionParser().ParseExpression("haogan() == 60"), environment).AsBoolean("test"));
    }

    [Fact]
    public void SaveGame_RoundTripsTargetedFavorability()
    {
        var adventure = new AdventureState();
        adventure.ChangeFavorability(-4);
        adventure.ChangeFavorability("李文秀", 7);
        var saveGame = SaveGame.Create(
            adventure,
            new Party(),
            new Inventory(),
            new ChestState(),
            new EquipmentInstanceFactory(),
            new CurrencyState(),
            new ClockState(),
            new LocationState(),
            new MapEventProgressState(),
            new WorldTriggerState());

        var json = JsonSerializer.Serialize(saveGame, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<SaveGame>(json, GameJson.Default);

        Assert.NotNull(roundTripped);
        var restored = roundTripped.RestoreAdventureState();
        Assert.Equal(46, restored.GetFavorability());
        Assert.Equal(57, restored.GetFavorability("李文秀"));
        Assert.Equal(50, restored.GetFavorability("未记录角色"));
    }

    private sealed class ThrowingRuntimeHost : IRuntimeHost
    {
        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
            ValueTask.FromException(new InvalidOperationException("Dialogue should not be invoked."));

        public ValueTask<ExpressionValue> GetVariableAsync(string name, CancellationToken cancellationToken) =>
            ValueTask.FromException<ExpressionValue>(new InvalidOperationException("Variable fallback should not be invoked."));

        public ValueTask<bool> EvaluatePredicateAsync(
            string name,
            IReadOnlyList<ExpressionValue> args,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<bool>(new InvalidOperationException("Predicate fallback should not be invoked."));

        public ValueTask<StoryCommandResult> ExecuteCommandAsync(
            string name,
            IReadOnlyList<ExpressionValue> args,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<StoryCommandResult>(new InvalidOperationException("Command fallback should not be invoked."));

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) =>
            ValueTask.FromException<int>(new InvalidOperationException("Choice should not be invoked."));

        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
            ValueTask.FromException<BattleOutcome>(new InvalidOperationException("Battle should not be invoked."));
    }
}
