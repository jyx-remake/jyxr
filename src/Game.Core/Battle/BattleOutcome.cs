namespace Game.Core.Battle;

public enum BattleOutcomeKind
{
    Ongoing,
    Winner,
    Draw,
}

public sealed record BattleOutcome(BattleOutcomeKind Kind, int? WinningTeam = null)
{
    public static BattleOutcome Ongoing { get; } = new(BattleOutcomeKind.Ongoing);
    public static BattleOutcome Draw { get; } = new(BattleOutcomeKind.Draw);
}

public static class BattleOutcomeEvaluator
{
    public static BattleOutcome Evaluate(BattleState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var livingTeams = state.Units
            .Where(static unit => unit.IsAlive)
            .Select(static unit => unit.Team)
            .Distinct()
            .Take(2)
            .ToArray();

        return livingTeams.Length switch
        {
            0 => BattleOutcome.Draw,
            1 => new BattleOutcome(BattleOutcomeKind.Winner, livingTeams[0]),
            _ => BattleOutcome.Ongoing,
        };
    }
}
