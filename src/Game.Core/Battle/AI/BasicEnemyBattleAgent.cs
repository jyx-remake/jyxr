namespace Game.Core.Battle;

public sealed class BasicEnemyBattleAgent : IBattleAgent
{
    private readonly BattleTurnCandidateGenerator _candidateGenerator;

    public BasicEnemyBattleAgent(BattleTurnCandidateGenerator candidateGenerator)
    {
        _candidateGenerator = candidateGenerator ?? throw new ArgumentNullException(nameof(candidateGenerator));
    }

    public BattleTurnPlan Decide(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);

        var unit = state.GetUnit(unitId);
        var isLowHp = unit.MaxHp > 0 && (double)unit.Hp / unit.MaxHp < 0.3d;
        var restRecovery = ResolveRestRecovery(unit);
        var candidates = _candidateGenerator.Generate(state, unitId)
            .Select(candidate => candidate with
            {
                Score = ScoreCandidate(candidate, isLowHp, restRecovery),
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.EnemyKills)
            .ThenByDescending(candidate => candidate.EnemyDamage)
            .ThenBy(candidate => candidate.AllyDamage)
            .ThenBy(candidate => isLowHp ? -candidate.DistanceToNearestEnemy : candidate.DistanceToNearestEnemy)
            .ThenBy(candidate => candidate.Plan.MoveDestination.Y)
            .ThenBy(candidate => candidate.Plan.MoveDestination.X)
            .ThenBy(candidate => candidate.Plan.MainAction.TargetPosition?.Y ?? int.MinValue)
            .ThenBy(candidate => candidate.Plan.MainAction.TargetPosition?.X ?? int.MinValue)
            .ThenBy(candidate => candidate.Plan.MainAction.SkillId ?? string.Empty, StringComparer.Ordinal)
            .ToArray();

        return candidates.Length == 0
            ? new BattleTurnPlan(unitId, unit.Position, BattleMainActionPlan.Rest())
            : candidates[0].Plan;
    }

    private static double ScoreCandidate(BattleTurnCandidate candidate, bool isLowHp, int restRecovery)
    {
        var score = (double)(candidate.EnemyDamage - candidate.AllyDamage);
        score += candidate.EnemyKills * 5000d;
        score -= candidate.AllyKills * 8000d;
        if (candidate.EnemyHitCount > 1)
        {
            score += (candidate.EnemyHitCount - 1) * 400d;
        }

        if (candidate.Plan.MainAction.Kind == BattleMainActionKind.Rest)
        {
            score += restRecovery;
            score += isLowHp ? 1500d : -800d;
        }

        score += isLowHp
            ? candidate.DistanceToNearestEnemy * 120d
            : -candidate.DistanceToNearestEnemy * 40d;
        return score;
    }

    private static int ResolveRestRecovery(BattleUnit unit)
    {
        var recovery = BattleRestCalculator.EstimateAverage(unit);
        return recovery.Hp + recovery.Mp;
    }
}
