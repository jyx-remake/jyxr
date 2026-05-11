using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Application;

public sealed record EquipmentAffixGroup(
    int StartIndex,
    int Count,
    IReadOnlyList<AffixDefinition> Affixes);

public static class EquipmentAffixGroups
{
    public static IReadOnlyList<EquipmentAffixGroup> Group(IReadOnlyList<AffixDefinition> affixes)
    {
        ArgumentNullException.ThrowIfNull(affixes);

        var groups = new List<EquipmentAffixGroup>();
        for (var index = 0; index < affixes.Count; index++)
        {
            var count = IsMergedPairStart(affixes, index) ? 2 : 1;
            groups.Add(new EquipmentAffixGroup(
                index,
                count,
                affixes.Skip(index).Take(count).ToArray()));
            index += count - 1;
        }

        return groups;
    }

    public static int Count(IReadOnlyList<AffixDefinition> affixes) => Group(affixes).Count;

    private static bool IsMergedPairStart(IReadOnlyList<AffixDefinition> affixes, int index)
    {
        if (index + 1 >= affixes.Count)
        {
            return false;
        }

        return affixes[index] switch
        {
            StatModifierAffix attack when attack.Stat == StatType.Attack
                && affixes[index + 1] is StatModifierAffix critChance
                && critChance.Stat == StatType.CritChance => true,

            StatModifierAffix defence when defence.Stat == StatType.Defence
                && affixes[index + 1] is StatModifierAffix antiCritChance
                && antiCritChance.Stat == StatType.AntiCritChance => true,

            SkillBonusModifierAffix legendSkillBonus
                when affixes[index + 1] is LegendSkillChanceModifierAffix legendSkillChance
                && string.Equals(legendSkillBonus.SkillId, legendSkillChance.SkillId, StringComparison.Ordinal) => true,

            _ => false,
        };
    }
}
