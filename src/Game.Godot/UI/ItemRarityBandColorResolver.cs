using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public static class ItemRarityBandColorResolver
{
    private static readonly Color White = new(1f, 1f, 1f, 0.3f);
    private static readonly Color Blue = new(0f, 0f, 1f, 0.3f);
    private static readonly Color Green = new(0f, 1f, 0f, 0.3f);
    private static readonly Color Yellow = new(1f, 1f, 0f, 0.3f);
    private static readonly Color Magenta = new(1f, 0f, 1f, 0.3f);
    private static readonly Color Red = new(1f, 0f, 0f, 0.3f);

    public static Color Resolve(InventoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return entry switch
        {
            EquipmentInstanceInventoryEntry equipment => Resolve(equipment.Equipment),
            _ => Resolve(entry.Definition),
        };
    }

    public static Color Resolve(EquipmentInstance equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);

        return EquipmentAffixGroupCounter.Count(equipment.ExtraAffixes) switch
        {
            >= 4 => Magenta,
            3 => Yellow,
            2 => Green,
            1 => Blue,
            _ => White,
        };
    }

    public static Color Resolve(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item.Type switch
        {
            ItemType.Booster => Red,
            ItemType.TalentBook => Magenta,
            _ => White,
        };
    }
}
