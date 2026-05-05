using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Application;

public sealed class ChestService
{
    private readonly GameSession _session;
    private GameConfig Config => _session.Config;

    public ChestService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;
    private ChestState Chest => State.Chest;

    public ChestView Open()
    {
        var entries = Chest.Inventory.Entries
            .OrderBy(entry => entry.EntryNumber)
            .ToList();
        return new ChestView(entries, Chest.GetStoredItemCount(), GetCapacity());
    }

    public ChestTransactionResult Deposit(InventoryEntry entry, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (!CanStore(entry.Definition))
        {
            return ChestTransactionResult.Failed($"【{entry.Definition.Name}】不能放入储物箱。");
        }

        if (Chest.GetStoredItemCount() + quantity > GetCapacity())
        {
            return ChestTransactionResult.Failed("储物箱已经满了。");
        }

        switch (entry)
        {
            case StackInventoryEntry stack:
                if (quantity > stack.Quantity)
                {
                    return ChestTransactionResult.Failed($"【{entry.Definition.Name}】数量不足。");
                }

                State.Inventory.RemoveItem(stack.Definition, quantity);
                Chest.Inventory.AddItem(stack.Definition, quantity);
                break;

            case EquipmentInstanceInventoryEntry equipment:
                if (quantity != 1)
                {
                    return ChestTransactionResult.Failed("独立装备一次只能存入 1 件。");
                }

                var removedEquipment = State.Inventory.RemoveEquipmentInstance(equipment.Equipment.Id);
                Chest.Inventory.AddEquipmentInstance(removedEquipment);
                break;

            default:
                throw new InvalidOperationException($"Unsupported inventory entry type '{entry.GetType().Name}'.");
        }

        PublishChanged();
        return ChestTransactionResult.Succeeded($"存入【{entry.Definition.Name}】 x{quantity}");
    }

    public ChestTransactionResult Withdraw(InventoryEntry entry, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        switch (entry)
        {
            case StackInventoryEntry stack:
                if (quantity > stack.Quantity)
                {
                    return ChestTransactionResult.Failed($"【{entry.Definition.Name}】数量不足。");
                }

                Chest.Inventory.RemoveItem(stack.Definition, quantity);
                State.Inventory.AddItem(stack.Definition, quantity);
                break;

            case EquipmentInstanceInventoryEntry equipment:
                if (quantity != 1)
                {
                    return ChestTransactionResult.Failed("独立装备一次只能取出 1 件。");
                }

                var removedEquipment = Chest.Inventory.RemoveEquipmentInstance(equipment.Equipment.Id);
                State.Inventory.AddEquipmentInstance(removedEquipment);
                break;

            default:
                throw new InvalidOperationException($"Unsupported inventory entry type '{entry.GetType().Name}'.");
        }

        PublishChanged();
        return ChestTransactionResult.Succeeded($"取出【{entry.Definition.Name}】 x{quantity}");
    }

    public bool CanStore(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Type != ItemType.QuestItem;
    }

    public int GetCapacity() => checked(Config.ChestBaseCapacity + State.Adventure.Round * Config.ChestPerRoundCapacity);

    private void PublishChanged()
    {
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new ChestChangedEvent());
    }
}

public sealed record ChestView(
    IReadOnlyList<InventoryEntry> Entries,
    int StoredItemCount,
    int Capacity);

public sealed record ChestTransactionResult(
    bool Success,
    string Message)
{
    public static ChestTransactionResult Succeeded(string message) => new(true, message);

    public static ChestTransactionResult Failed(string message) => new(false, message);
}
