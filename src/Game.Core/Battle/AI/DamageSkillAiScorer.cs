using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed class DamageSkillAiScorer : IBattleSkillAiScorer
{
    private readonly BattleDamageEstimator _damageEstimator;

    public DamageSkillAiScorer(BattleDamageEstimator? damageEstimator = null)
    {
        _damageEstimator = damageEstimator ?? new BattleDamageEstimator();
    }

    public bool CanScore(SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return skill is not SpecialSkillInstance && skill.Power > 0d;
    }

    public BattleSkillAiEvaluation Score(BattleSkillAiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var enemyDamage = 0;
        var allyDamage = 0;
        var enemyKills = 0;
        var allyKills = 0;
        var enemyHitCount = 0;

        foreach (var target in context.Targets)
        {
            var estimate = _damageEstimator.Estimate(context.Source, target, context.Skill);
            if (estimate.IsEnemy)
            {
                enemyDamage += estimate.Damage;
                if (estimate.Damage > 0)
                {
                    enemyHitCount++;
                }

                if (estimate.IsKill)
                {
                    enemyKills++;
                }
            }
            else
            {
                allyDamage += estimate.Damage;
                if (estimate.IsKill)
                {
                    allyKills++;
                }
            }
        }

        return new BattleSkillAiEvaluation(
            enemyDamage,
            allyDamage,
            enemyKills,
            allyKills,
            enemyHitCount);
    }
}
