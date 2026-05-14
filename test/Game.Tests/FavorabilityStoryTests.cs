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
    public async Task HaoganCommand_SupportsDefaultAndTargetedFavorability()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var dispatcher = new StoryCommandDispatcher(session, new ThrowingRuntimeHost());

        await dispatcher.ExecuteCommandAsync("haogan", [ExprValue.FromNumber(3)], default);
        await dispatcher.ExecuteCommandAsync(
            "haogan",
            [ExprValue.FromString("李文秀"), ExprValue.FromNumber(5)],
            default);

        Assert.Equal(53, session.State.Adventure.GetFavorability());
        Assert.Equal(55, session.State.Adventure.GetFavorability("李文秀"));
        Assert.Equal(50, session.State.Adventure.GetFavorability("长安一梦阿玉"));
    }

    [Fact]
    public async Task HaoganPredicates_SupportDefaultAndTargetedFavorability()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        session.State.Adventure.ChangeFavorability(10);
        session.State.Adventure.ChangeFavorability("李文秀", 5);
        var evaluator = new StoryConditionEvaluator(session, new ThrowingRuntimeHost());

        Assert.True(await evaluator.EvaluatePredicateAsync("haogan_more_than", [ExprValue.FromNumber(60)], default));
        Assert.False(await evaluator.EvaluatePredicateAsync("haogan_less_than", [ExprValue.FromNumber(60)], default));
        Assert.False(await evaluator.EvaluatePredicateAsync(
            "haogan_more_than",
            [ExprValue.FromString("李文秀"), ExprValue.FromNumber(60)],
            default));
        Assert.True(await evaluator.EvaluatePredicateAsync(
            "haogan_less_than",
            [ExprValue.FromString("李文秀"), ExprValue.FromNumber(60)],
            default));
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

        public ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken) =>
            ValueTask.FromException<ExprValue>(new InvalidOperationException("Variable fallback should not be invoked."));

        public ValueTask<bool> EvaluatePredicateAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<bool>(new InvalidOperationException("Predicate fallback should not be invoked."));

        public ValueTask<StoryCommandResult> ExecuteCommandAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<StoryCommandResult>(new InvalidOperationException("Command fallback should not be invoked."));

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) =>
            ValueTask.FromException<int>(new InvalidOperationException("Choice should not be invoked."));

        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
            ValueTask.FromException<BattleOutcome>(new InvalidOperationException("Battle should not be invoked."));
    }
}
