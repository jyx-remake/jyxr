using Game.Core.Affix;
using Game.Core.Definitions.Skills;
using Game.Core.Model.Character;

namespace Game.Core.Battle;

/// <summary>
/// Resolves a character's battle aura visual (legacy role_effect trigger).
/// Priority mirrors the legacy runtime: equipped internal skill, other
/// internal skills, active external skills, other external skills,
/// equipment, then the equipped title. Skill entries honor their level
/// gates; equipped-only entries additionally require the source skill to
/// be the equipped internal skill (or an active external skill).
/// </summary>
public static class RoleEffectResolver
{
    public static RoleEffectAffix? Resolve(CharacterInstance character)
    {
        ArgumentNullException.ThrowIfNull(character);

        var equippedInternalId = character.EquippedInternalSkillId;
        var equippedInternal = character.InternalSkills.FirstOrDefault(skill =>
            string.Equals(skill.Definition.Id, equippedInternalId, StringComparison.Ordinal));
        if (equippedInternal is not null)
        {
            foreach (var affix in equippedInternal.Definition.Affixes)
            {
                if (affix.RequiresEquippedInternalSkill &&
                    IsActiveRoleEffect(affix, equippedInternal.Level, equipped: true, active: true, out var roleEffect))
                {
                    return roleEffect;
                }
            }
        }

        foreach (var skill in character.InternalSkills)
        {
            foreach (var affix in skill.Definition.Affixes)
            {
                if (!affix.RequiresEquippedInternalSkill &&
                    IsActiveRoleEffect(affix, skill.Level, equipped: false, active: true, out var roleEffect))
                {
                    return roleEffect;
                }
            }
        }

        foreach (var skill in character.ExternalSkills)
        {
            foreach (var affix in skill.Definition.Affixes)
            {
                if (IsActiveRoleEffect(affix, skill.Level, equipped: false, skill.IsActive, out var roleEffect))
                {
                    return roleEffect;
                }
            }
        }

        foreach (var equipment in character.EquippedItems.Values)
        {
            foreach (var affix in equipment.Definition.Affixes)
            {
                if (affix is RoleEffectAffix roleEffect)
                {
                    return roleEffect;
                }
            }
        }

        foreach (var title in character.Titles.Where(title => title.Equipped))
        {
            foreach (var affix in title.Definition.Affixes)
            {
                if (affix is RoleEffectAffix roleEffect)
                {
                    return roleEffect;
                }
            }
        }

        return null;
    }

    private static bool IsActiveRoleEffect(
        SkillAffixDefinition affix,
        int skillLevel,
        bool equipped,
        bool active,
        out RoleEffectAffix? roleEffect)
    {
        roleEffect = null;
        if (affix.Effect is not RoleEffectAffix candidate)
        {
            return false;
        }

        if (skillLevel < affix.MinimumLevel)
        {
            return false;
        }

        if (affix.RequiresEquippedInternalSkill && !(equipped && active))
        {
            return false;
        }

        roleEffect = candidate;
        return true;
    }
}
