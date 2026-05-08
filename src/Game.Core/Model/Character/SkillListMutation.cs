using Game.Core.Model.Skills;

namespace Game.Core.Model.Character;

public readonly record struct SkillLevelChange<TSkill>(
    TSkill Skill,
    int OldLevel,
    int NewLevel,
    bool Created)
    where TSkill : SkillInstance;

internal static class SkillListMutation
{
    public static bool Remove<TSkill>(
        List<TSkill> skills,
        string skillId,
        Action<TSkill>? beforeRemove,
        Action? onChanged = null)
        where TSkill : SkillInstance
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        var index = FindIndex(skills, skillId);
        if (index < 0)
        {
            return false;
        }

        var skill = skills[index];
        beforeRemove?.Invoke(skill);
        skills.RemoveAt(index);
        onChanged?.Invoke();
        return true;
    }

    public static TSkill? Find<TSkill>(IEnumerable<TSkill> skills, string skillId)
        where TSkill : SkillInstance =>
        skills.FirstOrDefault(skill => string.Equals(skill.Id, skillId, StringComparison.Ordinal));

    public static TSkill GetRequired<TSkill>(IEnumerable<TSkill> skills, string skillId, string kind)
        where TSkill : SkillInstance =>
        Find(skills, skillId)
        ?? throw new InvalidOperationException($"{kind} '{skillId}' is not unlocked.");

    private static int FindIndex<TSkill>(List<TSkill> skills, string skillId)
        where TSkill : SkillInstance =>
        skills.FindIndex(skill => string.Equals(skill.Id, skillId, StringComparison.Ordinal));
}
