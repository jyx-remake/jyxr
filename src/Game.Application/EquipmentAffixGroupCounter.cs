using Game.Core.Affix;

namespace Game.Application;

public static class EquipmentAffixGroupCounter
{
    public static int Count(IReadOnlyList<AffixDefinition> affixes) =>
        EquipmentAffixGroups.Count(affixes);
}
