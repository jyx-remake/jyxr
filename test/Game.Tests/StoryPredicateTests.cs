using Game.Application;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Story;

namespace Game.Tests;

public sealed class StoryPredicateTests
{
    [Fact]
    public async Task CharacterAttributePredicates_UseLegacyCharacterThresholdArguments()
    {
        var state = new GameState();
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "主角",
            new Dictionary<StatType, int>
            {
                [StatType.Dingli] = 100,
                [StatType.Wuxing] = 119,
            });
        state.Party.AddMember(TestContentFactory.CreateCharacterInstance("主角", heroDefinition, state.EquipmentInstanceFactory));
        var session = new GameSession(state, TestContentFactory.CreateRepository(characters: [heroDefinition]));
        var evaluator = new StoryConditionEvaluator(session, new ThrowingRuntimeHost());

        Assert.True(await evaluator.EvaluatePredicateAsync(
            "dingli_greater_than",
            [ExprValue.FromString("主角"), ExprValue.FromNumber(100)],
            default));
        Assert.False(await evaluator.EvaluatePredicateAsync(
            "dingli_less_than",
            [ExprValue.FromString("主角"), ExprValue.FromNumber(100)],
            default));
        Assert.False(await evaluator.EvaluatePredicateAsync(
            "wuxing_greater_than",
            [ExprValue.FromString("主角"), ExprValue.FromNumber(120)],
            default));
        Assert.True(await evaluator.EvaluatePredicateAsync(
            "wuxing_less_than",
            [ExprValue.FromString("主角"), ExprValue.FromNumber(120)],
            default));
        Assert.False(await evaluator.EvaluatePredicateAsync(
            "dingli_less_than",
            [ExprValue.FromString("不存在"), ExprValue.FromNumber(100)],
            default));
    }

    [Fact]
    public async Task SkillLessThan_UsesLegacyCharacterSkillThresholdArguments()
    {
        var state = new GameState();
        var skill = TestContentFactory.CreateExternalSkill("血刀大法");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "主角",
            externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 9)]);
        state.Party.AddMember(TestContentFactory.CreateCharacterInstance("主角", heroDefinition, state.EquipmentInstanceFactory));
        var session = new GameSession(
            state,
            TestContentFactory.CreateRepository(characters: [heroDefinition], externalSkills: [skill]));
        var evaluator = new StoryConditionEvaluator(session, new ThrowingRuntimeHost());

        Assert.True(await evaluator.EvaluatePredicateAsync(
            "skill_less_than",
            [ExprValue.FromString("主角"), ExprValue.FromString("血刀大法"), ExprValue.FromNumber(10)],
            default));
        Assert.False(await evaluator.EvaluatePredicateAsync(
            "character_skill_less_than",
            [ExprValue.FromString("主角"), ExprValue.FromString("血刀大法"), ExprValue.FromNumber(9)],
            default));
        Assert.True(await evaluator.EvaluatePredicateAsync(
            "skill_less_than",
            [ExprValue.FromString("主角"), ExprValue.FromString("未学技能"), ExprValue.FromNumber(10)],
            default));
        Assert.False(await evaluator.EvaluatePredicateAsync(
            "skill_less_than",
            [ExprValue.FromString("不存在"), ExprValue.FromString("血刀大法"), ExprValue.FromNumber(10)],
            default));
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
