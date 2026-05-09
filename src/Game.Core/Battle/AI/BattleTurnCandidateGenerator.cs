using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed class BattleTurnCandidateGenerator
{
    private readonly BattleEngine _engine;
    private readonly IReadOnlyList<IBattleSkillAiScorer> _skillScorers;

    public BattleTurnCandidateGenerator(
        BattleEngine engine,
        IReadOnlyList<IBattleSkillAiScorer>? skillScorers = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _skillScorers = skillScorers ?? [new DamageSkillAiScorer()];
    }

    public IReadOnlyList<BattleTurnCandidate> Generate(BattleState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);

        var unit = state.GetUnit(unitId);
        var reachablePositions = _engine.GetReachablePositions(state, unitId).Keys
            .Append(unit.Position)
            .Distinct()
            .ToArray();
        var candidates = new List<BattleTurnCandidate>();

        foreach (var destination in reachablePositions)
        {
            foreach (var skill in BattleSkillCatalog.CollectSelectableSkills(unit))
            {
                var availability = _engine.EvaluateSkillAvailability(state, unit.Id, skill);
                if (skill is SpecialSkillInstance || !availability.IsAvailable)
                {
                    continue;
                }

                var scorer = _skillScorers.FirstOrDefault(candidateScorer => candidateScorer.CanScore(skill));
                if (scorer is null)
                {
                    continue;
                }

                foreach (var target in BattleSkillTargeting.EnumerateCastTargets(destination, skill.CastSize, state.Grid))
                {
                    var impactedPositions = BattleEngine.GetImpactPositions(destination, target, skill.ImpactType, skill.ImpactSize)
                        .Where(state.Grid.Contains)
                        .ToHashSet();
                    var targets = ResolveTargetsAtPosition(state, unit, skill, impactedPositions);
                    if (targets.Count == 0 || targets.All(targetUnit => !state.AreEnemies(unit, targetUnit)))
                    {
                        continue;
                    }

                    var evaluation = scorer.Score(new BattleSkillAiContext(
                        state,
                        unit,
                        skill,
                        destination,
                        target,
                        targets));
                    candidates.Add(new BattleTurnCandidate(
                        new BattleTurnPlan(unit.Id, destination, BattleMainActionPlan.CastSkill(skill.Id, target)),
                        Score: 0d,
                        evaluation.EnemyDamage,
                        evaluation.AllyDamage,
                        evaluation.EnemyKills,
                        evaluation.AllyKills,
                        evaluation.EnemyHitCount,
                        DistanceToNearestEnemy: GetDistanceToNearestEnemy(state, unit, destination)));
                }
            }

            candidates.Add(new BattleTurnCandidate(
                new BattleTurnPlan(unit.Id, destination, BattleMainActionPlan.Rest()),
                Score: 0d,
                EnemyDamage: 0,
                AllyDamage: 0,
                EnemyKills: 0,
                AllyKills: 0,
                EnemyHitCount: 0,
                DistanceToNearestEnemy: GetDistanceToNearestEnemy(state, unit, destination)));
        }

        return candidates;
    }

    private static IReadOnlyList<BattleUnit> ResolveTargetsAtPosition(
        BattleState state,
        BattleUnit source,
        SkillInstance skill,
        IReadOnlySet<GridPosition> impactedPositions)
    {
        if (impactedPositions.Count == 0)
        {
            return [];
        }

        return BattleSkillTargeting.ResolveEffectiveTargets(state, source, skill, impactedPositions);
    }

    private static int GetDistanceToNearestEnemy(BattleState state, BattleUnit unit, GridPosition destination)
    {
        var nearestEnemyDistance = state.GetLivingUnits()
            .Where(other => state.AreEnemies(unit, other))
            .Select(other => destination.ManhattanDistanceTo(other.Position))
            .DefaultIfEmpty(0)
            .Min();
        return nearestEnemyDistance;
    }
}
