using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Tests;

public sealed class BattleOutcomeEvaluatorTests
{
    [Fact]
    public void Evaluate_ReturnsOngoing_WhenMultipleTeamsAreAlive()
    {
        var state = CreateState(CreateUnit("a", 1, 0), CreateUnit("b", 2, 1));

        Assert.Equal(BattleOutcomeKind.Ongoing, BattleOutcomeEvaluator.Evaluate(state).Kind);
    }

    [Fact]
    public void Evaluate_ReturnsWinner_WhenOnlyOneTeamIsAlive()
    {
        var winner = CreateUnit("a", 3, 0);
        var defeated = CreateUnit("b", 4, 1);
        defeated.TakeDamage(defeated.MaxHp);
        var state = CreateState(winner, defeated);

        Assert.Equal(new BattleOutcome(BattleOutcomeKind.Winner, 3), BattleOutcomeEvaluator.Evaluate(state));
    }

    [Fact]
    public void Evaluate_ReturnsDraw_WhenAllUnitsAreDefeated()
    {
        var first = CreateUnit("a", 1, 0);
        var second = CreateUnit("b", 2, 1);
        first.TakeDamage(first.MaxHp);
        second.TakeDamage(second.MaxHp);

        Assert.Equal(BattleOutcome.Draw, BattleOutcomeEvaluator.Evaluate(CreateState(first, second)));
    }

    private static BattleState CreateState(params BattleUnit[] units) =>
        new(new BattleGrid(2, 1), units);

    private static BattleUnit CreateUnit(string id, int team, int x)
    {
        var definition = TestContentFactory.CreateCharacterDefinition(id);
        var character = TestContentFactory.CreateCharacterInstance(id, definition);
        return new BattleUnit(id, character, team, new GridPosition(x, 0));
    }
}
