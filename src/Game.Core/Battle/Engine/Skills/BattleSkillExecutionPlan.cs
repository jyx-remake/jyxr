using Game.Core.Model.Skills;

namespace Game.Core.Battle;

internal abstract record BattleSkillExecutionStep;

internal sealed record ResolveSkillDamageStep : BattleSkillExecutionStep;

internal sealed record ApplySkillBuffsStep : BattleSkillExecutionStep;

internal sealed record ApplyDefinedSkillEffectsStep(
    IReadOnlyList<BattleEffectDefinition> Effects) : BattleSkillExecutionStep;

internal sealed record AttemptAttackRageGainStep : BattleSkillExecutionStep;

internal sealed record BattleSkillExecutionPlan(
    SkillInstance Skill,
    IReadOnlyList<BattleSkillExecutionStep> Steps);

internal static class BattleSkillExecutionPlanFactory
{
    public static BattleSkillExecutionPlan Create(SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        var steps = new List<BattleSkillExecutionStep>();
        if (skill is SpecialSkillInstance specialSkill)
        {
            if (specialSkill.Buffs.Count > 0)
            {
                steps.Add(new ApplySkillBuffsStep());
            }

            if (specialSkill.Definition.Effects is { Count: > 0 } effects)
            {
                steps.Add(new ApplyDefinedSkillEffectsStep(effects));
            }

            if (specialSkill.Definition.Intent == SpecialSkillIntent.Offensive)
            {
                steps.Add(new AttemptAttackRageGainStep());
            }
        }
        else
        {
            steps.Add(new ResolveSkillDamageStep());
            steps.Add(new AttemptAttackRageGainStep());
        }

        return new BattleSkillExecutionPlan(skill, steps);
    }
}
