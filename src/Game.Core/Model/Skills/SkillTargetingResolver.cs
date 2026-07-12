using Game.Core.Affix;

namespace Game.Core.Model.Skills;

public static class SkillTargetingResolver
{
    public static int ResolveCastSize(SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return skill.Owner.GetSkillTargetingValue(
            skill.SourceSkillId,
            SkillTargetingField.CastSize,
            skill.CastSize);
    }

    public static int ResolveImpactSize(SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return skill.Owner.GetSkillTargetingValue(
            skill.SourceSkillId,
            SkillTargetingField.ImpactSize,
            skill.ImpactSize);
    }
}
