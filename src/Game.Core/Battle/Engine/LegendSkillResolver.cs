using Game.Core.Abstractions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;
using Game.Core;

namespace Game.Core.Battle;

public sealed class LegendSkillResolver
{
    public SkillInstance Resolve(
        IReadOnlyList<LegendSkillDefinition> definitions,
        SkillInstance baseSkill,
        IRandomService random)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(baseSkill);
        ArgumentNullException.ThrowIfNull(random);

        foreach (var definition in definitions)
        {
            if (!CanTrigger(definition, baseSkill))
            {
                continue;
            }

            var triggerChance = ResolveTriggerChance(definition, baseSkill.Owner);
            if (!Probability.RollChance(random, triggerChance))
            {
                continue;
            }

            return new LegendSkillInstance(definition, baseSkill);
        }

        return baseSkill;
    }

    private static bool CanTrigger(LegendSkillDefinition definition, SkillInstance baseSkill)
    {
        if (!string.Equals(definition.StartSkill, baseSkill.Id, StringComparison.Ordinal))
        {
            return false;
        }

        if (baseSkill.Level < definition.RequiredLevel)
        {
            return false;
        }

        return definition.Conditions.All(condition => IsConditionSatisfied(baseSkill.Owner, condition));
    }

    private static double ResolveTriggerChance(LegendSkillDefinition definition, CharacterInstance owner)
    {
        var wuxingMultiplier = 1d + owner.GetStat(StatType.Wuxing) / 150d * 0.2d;
        var chance = definition.Probability * wuxingMultiplier;
        chance = owner.GetLegendChanceValue(definition.Id, chance);
        return Math.Clamp(chance, 0d, 1d);
    }

    private static bool IsConditionSatisfied(CharacterInstance owner, LegendSkillConditionDefinition condition) =>
        condition switch
        {
            RequiredExternalSkillLevelLegendConditionDefinition externalSkill =>
                owner.GetExternalSkills().Any(skill =>
                    string.Equals(skill.Id, externalSkill.TargetId, StringComparison.Ordinal) &&
                    skill.Level >= externalSkill.Level),
            RequiredInternalSkillLevelLegendConditionDefinition internalSkill =>
                owner.GetInternalSkills().Any(skill =>
                    string.Equals(skill.Id, internalSkill.TargetId, StringComparison.Ordinal) &&
                    skill.Level >= internalSkill.Level),
            RequiredSpecialSkillLegendConditionDefinition specialSkill =>
                owner.GetSpecialSkills().Any(skill =>
                    string.Equals(skill.Id, specialSkill.TargetId, StringComparison.Ordinal)),
            RequiredTalentLegendConditionDefinition talent =>
                owner.HasTalent(talent.TargetId),
            _ => throw new NotSupportedException($"Unsupported legend skill condition '{condition.GetType().Name}'.")
        };
}
