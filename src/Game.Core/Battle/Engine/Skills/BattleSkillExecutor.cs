using Game.Core.Model.Skills;

namespace Game.Core.Battle;

internal sealed class BattleSkillExecutor(
    BattleEngine engine,
    BattleEffectExecutor effectExecutor)
{
    public void Execute(
        BattleState state,
        BattleUnit source,
        IReadOnlyList<BattleUnit> targets,
        BattleSkillExecutionPlan plan)
    {
        foreach (var step in plan.Steps)
        {
            using var stepScope = state.EnterEffect($"skill-step:{plan.Skill.Id}:{step.GetType().Name}");
            switch (step)
            {
                case ResolveSkillDamageStep:
                    engine.ExecuteSkillDamageStep(state, source, targets, plan.Skill);
                    break;
                case ApplySkillBuffsStep:
                    foreach (var target in targets)
                    {
                        engine.ApplySkillBuffs(state, source, target, plan.Skill.Buffs);
                    }
                    break;
                case ApplyDefinedSkillEffectsStep definedEffects:
                    ExecuteDefinedEffects(state, source, targets, plan.Skill, definedEffects.Effects);
                    break;
                case AttemptAttackRageGainStep:
                    engine.TryGainRageFromAttack(state, source, targets);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported skill execution step '{step.GetType().Name}'.");
            }
        }
    }

    private void ExecuteDefinedEffects(
        BattleState state,
        BattleUnit source,
        IReadOnlyList<BattleUnit> targets,
        SkillInstance skill,
        IReadOnlyList<BattleEffectDefinition>? effects)
    {
        if (effects is null) return;
        foreach (var effect in effects)
        {
            using var effectScope = state.EnterEffect($"skill:{effect.GetType().Name}");
            effectExecutor.ExecuteAbility(state, source, targets, effect, skill);
        }
    }
}
