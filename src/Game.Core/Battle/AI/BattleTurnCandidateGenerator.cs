using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Core.Definitions.Skills;

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
        _skillScorers = skillScorers ?? [new DamageSkillAiScorer(), new SpecialSkillAiScorer()];
    }

    public IReadOnlyList<BattleTurnCandidate> Generate(
        BattleState state,
        string unitId,
        BattleTurnCandidateGenerationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);

        options ??= BattleTurnCandidateGenerationOptions.Default;
        var unit = state.GetUnit(unitId);
        var reachablePositions = options.AllowMovement
            ? _engine.GetReachablePositions(state, unitId).Keys
                .Append(unit.Position)
                .Distinct()
                .ToArray()
            : [unit.Position];
        var candidates = new List<BattleTurnCandidate>();

        foreach (var destination in reachablePositions)
        {
            if (options.AllowSkillCandidates)
            {
                foreach (var skill in BattleSkillCatalog.CollectSelectableSkills(unit))
                {
                    if (options.SkillFilter is not null && !options.SkillFilter(skill))
                    {
                        continue;
                    }

                    if (skill is SpecialSkillInstance specialSkill &&
                        specialSkill.Definition.Intent != SpecialSkillIntent.Offensive)
                    {
                        continue;
                    }

                    var availability = _engine.EvaluateSkillAvailability(state, unit.Id, skill);
                    if (!availability.IsAvailable)
                    {
                        continue;
                    }

                    var scorer = _skillScorers.FirstOrDefault(candidateScorer => candidateScorer.CanScore(skill));
                    if (scorer is null)
                    {
                        continue;
                    }

                    var castSize = BattleSkillTargeting.ResolveEffectiveCastSize(unit, skill);
                    var impactSize = BattleSkillTargeting.ResolveEffectiveImpactSize(unit, skill);
                    foreach (var target in BattleSkillTargeting.EnumerateCastTargets(
                                 destination,
                                 castSize,
                                 skill.CanCastAtSelf,
                                 state.Grid))
                    {
                        var impactedPositions = BattleEngine.GetImpactPositions(destination, target, skill.ImpactType, impactSize)
                            .Where(state.Grid.Contains)
                            .ToHashSet();
						var targets = BattleSkillTargeting.ResolveEffectiveTargets(
							state,
							unit,
							skill,
							impactedPositions);
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
                        if (skill is SpecialSkillInstance && evaluation.EnemyDamage <= 0)
                        {
                            continue;
                        }
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
            }

            if (options.AllowRestCandidates)
            {
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
        }

        return candidates;
    }

    public BattleTurnPlan? CreateRandomSupportSpecialSkillPlan(
        BattleState state,
        string unitId,
        GridPosition destination)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);

        var unit = state.GetUnit(unitId);
        var skills = unit.Character.GetSpecialSkills()
            .Where(skill =>
                skill.IsActive &&
                skill.Definition.Intent == SpecialSkillIntent.Support &&
                skill.CanCastAtSelf &&
                _engine.EvaluateSkillAvailability(state, unit.Id, skill).IsAvailable)
            .ToArray();
        if (skills.Length == 0)
        {
            return null;
        }

        var skill = skills[Random.Shared.Next(skills.Length)];
        return new BattleTurnPlan(
            unit.Id,
            destination,
            BattleMainActionPlan.CastSkill(skill.Id, destination));
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
