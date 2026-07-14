using Game.Core.Definitions.Skills;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed class SpecialSkillAiScorer : IBattleSkillAiScorer
{
    public bool CanScore(SkillInstance skill) =>
        skill is SpecialSkillInstance
        {
            Definition.Intent: SpecialSkillIntent.Offensive,
            Definition.Effects: { Count: > 0 } effects
        } && effects.OfType<CustomAbilityBattleEffectDefinition>()
            .Any(effect => effect.Target is TargetBattleTargetSelectorDefinition);

    public BattleSkillAiEvaluation Score(BattleSkillAiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var skill = context.Skill as SpecialSkillInstance
            ?? throw new InvalidOperationException("Special skill scorer requires a special skill.");
        var effects = (skill.Definition.Effects ?? [])
            .OfType<CustomAbilityBattleEffectDefinition>()
            .Where(effect => effect.Target is TargetBattleTargetSelectorDefinition)
            .ToArray();
        var enemyDamage = 0;
        var allyDamage = 0;
        var enemyKills = 0;
        var allyKills = 0;
        var enemyHitCount = 0;

        foreach (var target in context.Targets)
        {
            var damage = effects.Sum(effect =>
                effect.EstimateAbilityDamage(new BattleAbilityDamageEstimateContext(
                    context.State,
                    context.Source,
                    target,
                    skill,
                    context.MoveDestination)) ?? 0);
            if (context.State.AreEnemies(context.Source, target))
            {
                enemyDamage += damage;
                if (damage > 0)
                {
                    enemyHitCount++;
                }

                if (damage >= target.Hp)
                {
                    enemyKills++;
                }
            }
            else
            {
                allyDamage += damage;
                if (damage >= target.Hp)
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
