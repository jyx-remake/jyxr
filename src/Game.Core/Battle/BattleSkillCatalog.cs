using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public static class BattleSkillCatalog
{
    public static IReadOnlyList<SkillInstance> CollectSelectableSkills(BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var character = unit.Character;
        return character.GetSpecialSkills()
            .Where(static skill => skill.IsActive)
            .Cast<SkillInstance>()
            .Concat(character.GetExternalSkills()
                .Where(static skill => skill.IsActive)
                .SelectMany(static skill => new[] { (SkillInstance)skill }
                    .Concat(skill.GetFormSkills().Where(static formSkill => formSkill.IsActive))))
            .Concat(character.GetInternalSkills()
                .Where(static skill => skill.IsEquipped)
                .SelectMany(static skill => skill.GetFormSkills().Where(static formSkill => formSkill.IsActive)))
            .ToList();
    }
}
