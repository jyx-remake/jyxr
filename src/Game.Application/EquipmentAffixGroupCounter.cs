using Game.Core.Affix;

namespace Game.Application;

public static class EquipmentAffixGroupCounter
{
    public static int Count(IReadOnlyList<AffixDefinition> affixes)
    {
        ArgumentNullException.ThrowIfNull(affixes);

        var count = 0;
        for (var index = 0; index < affixes.Count; index++)
        {
            if (IsMergedPairStart(affixes, index))
            {
                count++;
                index++;
                continue;
            }

            count++;
        }

        return count;
    }

    private static bool IsMergedPairStart(IReadOnlyList<AffixDefinition> affixes, int index)
    {
        if (index + 1 >= affixes.Count)
        {
            return false;
        }

        return affixes[index] switch
        {
            StatModifierAffix attack when attack.Stat == Game.Core.Model.StatType.Attack
                && affixes[index + 1] is StatModifierAffix critChance
                && critChance.Stat == Game.Core.Model.StatType.CritChance => true,

            StatModifierAffix defence when defence.Stat == Game.Core.Model.StatType.Defence
                && affixes[index + 1] is StatModifierAffix antiCritChance
                && antiCritChance.Stat == Game.Core.Model.StatType.AntiCritChance => true,

            SkillBonusModifierAffix legendSkillBonus
                when affixes[index + 1] is LegendSkillChanceModifierAffix legendSkillChance
                && string.Equals(legendSkillBonus.SkillId, legendSkillChance.SkillId, StringComparison.Ordinal) => true,

            _ => false,
        };
    }
}
