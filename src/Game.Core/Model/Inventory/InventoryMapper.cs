using Game.Core.Abstractions;
using Game.Core.Persistence;

namespace Game.Core.Model;

public static class InventoryMapper
{
    public static Inventory FromRecord(InventoryRecord record, IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(contentRepository);

        var entries = record.Entries.Select(entry => FromEntryRecord(entry, contentRepository)).ToList();
        return Inventory.Restore(record.NextEntryNumber, entries);
    }

    public static InventoryRecord ToRecord(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        return new InventoryRecord(
            inventory.NextEntryNumber,
            inventory.Entries.Select(ToEntryRecord).ToList());
    }

    private static InventoryEntry FromEntryRecord(InventoryEntryRecord record, IContentRepository contentRepository) =>
        record switch
        {
            StackInventoryEntryRecord stack => new StackInventoryEntry(
                stack.EntryNumber,
                contentRepository.GetItem(stack.ItemDefinitionId),
                stack.Quantity),
            EquipmentInstanceInventoryEntryRecord equipment => new EquipmentInstanceInventoryEntry(
                equipment.EntryNumber,
                EquipmentMapper.FromRecord(equipment.Equipment, contentRepository)),
            _ => throw new InvalidOperationException($"Unsupported inventory entry record type '{record.GetType().Name}'."),
        };

    private static InventoryEntryRecord ToEntryRecord(InventoryEntry entry) =>
        entry switch
        {
            StackInventoryEntry stack => new StackInventoryEntryRecord(
                stack.EntryNumber,
                stack.Definition.Id,
                stack.Quantity),
            EquipmentInstanceInventoryEntry equipment => new EquipmentInstanceInventoryEntryRecord(
                equipment.EntryNumber,
                EquipmentMapper.ToRecord(equipment.Equipment)),
            _ => throw new InvalidOperationException($"Unsupported inventory entry type '{entry.GetType().Name}'."),
        };
}
