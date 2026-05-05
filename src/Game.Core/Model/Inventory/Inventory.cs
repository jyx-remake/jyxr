using Game.Core.Definitions;

namespace Game.Core.Model;

public sealed class Inventory
{
    private readonly List<InventoryEntry> _entries = [];
    private readonly Dictionary<string, StackInventoryEntry> _stackEntriesByItemId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EquipmentInstanceInventoryEntry> _equipmentEntriesByInstanceId = new(StringComparer.Ordinal);
    private long _nextEntryNumber = 1;

    public IReadOnlyList<InventoryEntry> Entries => _entries;

    public long NextEntryNumber => _nextEntryNumber;

    public static Inventory Restore(
        long nextEntryNumber,
        IEnumerable<InventoryEntry> entries)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nextEntryNumber, 1);
        ArgumentNullException.ThrowIfNull(entries);

        var inventory = new Inventory
        {
            _nextEntryNumber = nextEntryNumber,
        };

        foreach (var entry in entries)
        {
            inventory.AddRestoredEntry(entry);
        }

        return inventory;
    }

    public StackInventoryEntry AddItem(ItemDefinition definition, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (_stackEntriesByItemId.TryGetValue(definition.Id, out var existing))
        {
            existing.AddQuantity(quantity);
            return existing;
        }

        var entry = new StackInventoryEntry(TakeNextEntryNumber(), definition, quantity);
        _entries.Add(entry);
        _stackEntriesByItemId.Add(definition.Id, entry);
        return entry;
    }

    public void RemoveItem(ItemDefinition definition, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (!_stackEntriesByItemId.TryGetValue(definition.Id, out var entry))
        {
            throw new InvalidOperationException($"Inventory does not contain item '{definition.Id}'.");
        }

        entry.RemoveQuantity(quantity);
        if (entry.Quantity > 0)
        {
            return;
        }

        _stackEntriesByItemId.Remove(definition.Id);
        _entries.Remove(entry);
    }

    public InventoryEntry AddEquipmentInstance(EquipmentInstance equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);

        if (!equipment.HasInstanceLevelDifferences)
        {
            return AddItem(equipment.Definition);
        }

        if (_equipmentEntriesByInstanceId.ContainsKey(equipment.Id))
        {
            throw new InvalidOperationException($"Equipment instance '{equipment.Id}' already exists in inventory.");
        }

        var entry = new EquipmentInstanceInventoryEntry(TakeNextEntryNumber(), equipment);
        _entries.Add(entry);
        _equipmentEntriesByInstanceId.Add(equipment.Id, entry);
        return entry;
    }

    public EquipmentInstance RemoveEquipmentInstance(string equipmentInstanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(equipmentInstanceId);

        if (!_equipmentEntriesByInstanceId.TryGetValue(equipmentInstanceId, out var entry))
        {
            throw new InvalidOperationException($"Inventory does not contain equipment instance '{equipmentInstanceId}'.");
        }

        _equipmentEntriesByInstanceId.Remove(equipmentInstanceId);
        _entries.Remove(entry);
        return entry.Equipment;
    }

    public EquipmentInstanceInventoryEntry GetEquipmentInstanceEntry(string equipmentInstanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(equipmentInstanceId);
        return _equipmentEntriesByInstanceId[equipmentInstanceId];
    }

    public StackInventoryEntry GetStack(ItemDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return _stackEntriesByItemId[definition.Id];
    }

    public bool ContainsStack(ItemDefinition definition, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        return _stackEntriesByItemId.TryGetValue(definition.Id, out var entry) && entry.Quantity >= quantity;
    }

    private void AddRestoredEntry(InventoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        switch (entry)
        {
            case StackInventoryEntry stack:
                if (stack.Quantity <= 0)
                {
                    throw new InvalidOperationException($"Inventory stack '{stack.Definition.Id}' has invalid quantity '{stack.Quantity}'.");
                }

                if (!_stackEntriesByItemId.TryAdd(stack.Definition.Id, stack))
                {
                    throw new InvalidOperationException($"Inventory stack '{stack.Definition.Id}' is duplicated.");
                }
                break;

            case EquipmentInstanceInventoryEntry equipment:
                if (!equipment.Equipment.HasInstanceLevelDifferences)
                {
                    throw new InvalidOperationException($"Equipment instance '{equipment.Equipment.Id}' has no instance-level differences.");
                }

                if (!_equipmentEntriesByInstanceId.TryAdd(equipment.Equipment.Id, equipment))
                {
                    throw new InvalidOperationException($"Equipment instance '{equipment.Equipment.Id}' is duplicated.");
                }
                break;

            default:
                throw new InvalidOperationException($"Unsupported inventory entry type '{entry.GetType().Name}'.");
        }

        _entries.Add(entry);
    }

    private long TakeNextEntryNumber() => _nextEntryNumber++;
}

public abstract class InventoryEntry
{
    protected InventoryEntry(long entryNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(entryNumber, 1);
        EntryNumber = entryNumber;
    }

    public long EntryNumber { get; }

    public abstract ItemDefinition Definition { get; }
}

public sealed class StackInventoryEntry : InventoryEntry
{
    public StackInventoryEntry(long entryNumber, ItemDefinition item, int quantity)
        : base(entryNumber)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        Item = item;
        Quantity = quantity;
    }

    public ItemDefinition Item { get; }

    public int Quantity { get; private set; }

    public override ItemDefinition Definition => Item;

    public bool IsEquipmentStack => Item is EquipmentDefinition;

    public void AddQuantity(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        Quantity += quantity;
    }

    public void RemoveQuantity(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (Quantity < quantity)
        {
            throw new InvalidOperationException($"Inventory stack '{Item.Id}' does not contain enough items.");
        }

        Quantity -= quantity;
    }
}

public sealed class EquipmentInstanceInventoryEntry : InventoryEntry
{
    public EquipmentInstanceInventoryEntry(long entryNumber, EquipmentInstance equipment)
        : base(entryNumber)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        Equipment = equipment;
    }

    public EquipmentInstance Equipment { get; }

    public override ItemDefinition Definition => Equipment.Definition;
}
