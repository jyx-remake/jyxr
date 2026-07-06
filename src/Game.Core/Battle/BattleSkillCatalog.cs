using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public static class BattleSkillCatalog
{
    public static IReadOnlyList<SkillInstance> CollectSelectableSkills(BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var character = unit.Character;
        var externalSkills = character.GetExternalSkills();
        var internalSkills = character.GetInternalSkills();
        return character.GetSpecialSkills()
            .Where(static skill => skill.IsActive)
            .Cast<SkillInstance>()
            .Concat(externalSkills
                .Where(static skill => skill.IsActive)
                .Cast<SkillInstance>())
            .Concat(externalSkills
                .SelectMany(static skill => skill.GetFormSkills().Where(static formSkill => formSkill.IsActive)))
            .Concat(internalSkills
                .SelectMany(static skill => skill.GetFormSkills().Where(static formSkill => formSkill.IsActive)))
            .ToList();
    }
}
