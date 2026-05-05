using System.Text.Json.Serialization;

namespace Game.Core.Persistence;

public sealed record InventoryRecord(
    long NextEntryNumber,
    IReadOnlyList<InventoryEntryRecord> Entries);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(StackInventoryEntryRecord), "stack")]
[JsonDerivedType(typeof(EquipmentInstanceInventoryEntryRecord), "equipment_instance")]
public abstract record InventoryEntryRecord(long EntryNumber);

public sealed record StackInventoryEntryRecord(
    long EntryNumber,
    string ItemDefinitionId,
    int Quantity) : InventoryEntryRecord(EntryNumber);

public sealed record EquipmentInstanceInventoryEntryRecord(
    long EntryNumber,
    EquipmentRecord Equipment) : InventoryEntryRecord(EntryNumber);
