using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed record BattleSkillCastInfo(
    string BaseSkillId,
    string ResolvedSkillId,
    string ResolvedSkillName,
    SkillKind ResolvedSkillKind,
    bool IsLegend,
    string? ImpactAnimationId,
    string? ScreenEffectAnimationId,
    string? AudioId)
{
    public static BattleSkillCastInfo Create(SkillInstance baseSkill, SkillInstance resolvedSkill)
    {
        ArgumentNullException.ThrowIfNull(baseSkill);
        ArgumentNullException.ThrowIfNull(resolvedSkill);

        return new BattleSkillCastInfo(
            baseSkill.Id,
            resolvedSkill.Id,
            resolvedSkill.Name,
            resolvedSkill.SkillKind,
            resolvedSkill is LegendSkillInstance,
            resolvedSkill.Animation,
            resolvedSkill is LegendSkillInstance legendSkill ? legendSkill.ScreenEffectAnimation : null,
            resolvedSkill.Audio);
    }
}
