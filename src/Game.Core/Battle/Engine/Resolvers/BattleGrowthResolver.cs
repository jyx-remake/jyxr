using Game.Core.Definitions;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

internal sealed class BattleGrowthResolver(
    Func<SkillInstance, int> skillMaxLevelResolver,
    Func<CharacterInstance, GrowTemplateDefinition> characterGrowTemplateResolver,
    Func<CharacterInstance, int> characterMaxLevelResolver,
    Func<BattleUnit, bool> eligibilityResolver)
{
    private const int SkillCastCharacterExperience = 3;

    public void ApplySkillUseGrowth(BattleState state, BattleUnit unit, SkillInstance usedSkill)
    {
        if (!eligibilityResolver(unit)) return;

        var experience = SkillExperienceProgression.CalculateBattleUseExperience(unit.Character);
        var progressedSkills = new HashSet<SkillInstance>(ReferenceEqualityComparer.Instance);
        ApplySkillExperience(state, unit, usedSkill, experience, progressedSkills);

        var equippedInternalSkill = unit.Character.GetInternalSkills()
            .FirstOrDefault(static skill => skill.IsEquipped);
        if (equippedInternalSkill is not null)
        {
            ApplySkillExperience(state, unit, equippedInternalSkill, experience, progressedSkills);
        }

        ApplyCharacterExperience(state, unit);
    }

    private void ApplySkillExperience(
        BattleState state,
        BattleUnit unit,
        SkillInstance skill,
        int experience,
        HashSet<SkillInstance> progressedSkills)
    {
        if (SkillExperienceProgression.NormalizeProgressionSkill(skill) is not { } progressSkill ||
            !progressedSkills.Add(progressSkill)) return;

        var change = SkillExperienceProgression.TryAddExperience(
            progressSkill, experience, skillMaxLevelResolver(progressSkill));
        if (change is not { LeveledUp: true }) return;

        unit.ClampResourcesToLimits();
        BattleEngine.AddMessage(state, new BattleFact(
            BattleFactKind.SkillLeveledUp,
            unit.Id,
            skillExperience: new BattleSkillExperienceEvent(
                progressSkill.Id,
                progressSkill.Name,
                progressSkill.SkillKind,
                change.AddedExperience,
                change.OldLevel,
                change.NewLevel)));
    }

    private void ApplyCharacterExperience(BattleState state, BattleUnit unit)
    {
        var change = CharacterExperienceProgression.TryAddExperience(
            unit.Character,
            SkillCastCharacterExperience,
            characterMaxLevelResolver(unit.Character),
            () => characterGrowTemplateResolver(unit.Character));
        if (!change.LeveledUp) return;

        unit.ClampResourcesToLimits();
        BattleEngine.AddMessage(state, new BattleFact(
            BattleFactKind.CharacterLeveledUp,
            unit.Id,
            characterExperience: new BattleCharacterExperienceEvent(
                unit.Character.Id,
                unit.Character.Name,
                change.AddedExperience,
                change.OldLevel,
                change.NewLevel)));
    }
}
