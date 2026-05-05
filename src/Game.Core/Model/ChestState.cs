using Game.Core.Abstractions;
using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class ChestState
{
    public Inventory Inventory { get; private set; } = new();

    public static ChestState Restore(ChestStateRecord? record, IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);

        var state = new ChestState();
        if (record is null)
        {
            return state;
        }

        state.Inventory = InventoryMapper.FromRecord(record.Inventory, contentRepository);
        return state;
    }

    public ChestState Clone(IContentRepository contentRepository) =>
        Restore(ToRecord(), contentRepository);

    public int GetStoredItemCount() =>
        Inventory.Entries.Sum(static entry => entry is StackInventoryEntry stack ? stack.Quantity : 1);

    public void SetInventory(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        Inventory = inventory;
    }

    public ChestStateRecord ToRecord() =>
        new(InventoryMapper.ToRecord(Inventory));
}
