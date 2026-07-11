using Game.Core.Model.Skills;

namespace Game.Core.Battle;

internal abstract record BattleSkillExecutionStep;

internal sealed record ResolveSkillDamageStep : BattleSkillExecutionStep;

internal sealed record ApplySkillBuffsStep : BattleSkillExecutionStep;

internal sealed record ApplyDefinedSkillEffectsStep(
    IReadOnlyList<BattleEffectDefinition> Effects) : BattleSkillExecutionStep;

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
        }
        else
        {
            steps.Add(new ResolveSkillDamageStep());
        }

        return new BattleSkillExecutionPlan(skill, steps);
    }
}
