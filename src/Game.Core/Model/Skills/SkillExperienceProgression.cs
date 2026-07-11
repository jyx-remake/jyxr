using Game.Core.Model.Character;

namespace Game.Core.Model.Skills;

public sealed record SkillExperienceChange(
    SkillInstance Skill,
    int AddedExperience,
    int OldLevel,
    int NewLevel)
{
    public bool LeveledUp => NewLevel > OldLevel;
}

public static class SkillExperienceProgression
{
    public const int DefaultMaxLevel = 20;

    public static int CalculateBattleUseExperience(CharacterInstance owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var wuxing = Math.Floor(owner.GetStat(StatType.Wuxing));
        return checked(2 * (15 + (int)Math.Floor(wuxing / 2d) + (int)Math.Floor(wuxing / 30d)));
    }

    public static SkillExperienceChange? TryAddExperience(
        SkillInstance skill,
        int experience,
        int maxLevel)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentOutOfRangeException.ThrowIfNegative(experience);
        ValidateMaxLevel(maxLevel);

        var progressSkill = NormalizeProgressionSkill(skill);
        if (progressSkill is null)
        {
            return null;
        }

        var addedExperience = ExperienceGainPolicy.Resolve(progressSkill.Owner, experience);
        var oldLevel = progressSkill.Level;
        var effectiveMaxLevel = Math.Max(maxLevel, oldLevel);
        progressSkill.Exp = checked(progressSkill.Exp + addedExperience);
        while (progressSkill.Exp >= GetLevelUpExp(progressSkill))
        {
            if (progressSkill.Level < effectiveMaxLevel)
            {
                progressSkill.Exp -= GetLevelUpExp(progressSkill);
                progressSkill.Level += 1;
                continue;
            }

            progressSkill.Exp = GetLevelUpExp(progressSkill);
            break;
        }

        if (progressSkill.Level != oldLevel)
        {
            progressSkill.Owner.RebuildSnapshot();
        }

        return new SkillExperienceChange(progressSkill, addedExperience, oldLevel, progressSkill.Level);
    }

    public static SkillInstance? NormalizeProgressionSkill(SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        return skill switch
        {
            ExternalSkillInstance or InternalSkillInstance => skill,
            FormSkillInstance formSkill => NormalizeProgressionSkill(formSkill.Parent),
            LegendSkillInstance legendSkill => NormalizeProgressionSkill(legendSkill.Parent),
            _ => null,
        };
    }

    public static int GetLevelUpExp(SkillInstance skill) =>
        skill switch
        {
            ExternalSkillInstance externalSkill => externalSkill.LevelUpExp,
            InternalSkillInstance internalSkill => internalSkill.LevelUpExp,
            _ => throw new NotSupportedException($"Skill '{skill.GetType().Name}' does not have experience progression."),
        };

    private static void ValidateMaxLevel(int maxLevel) => ArgumentOutOfRangeException.ThrowIfLessThan(maxLevel, 1);
}
