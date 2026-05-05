using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public sealed class InventoryService
{
    private readonly GameSession _session;

    public InventoryService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;

    private Inventory Inventory => State.Inventory;

    private EquipmentInstanceFactory EquipmentInstanceFactory => State.EquipmentInstanceFactory;

    public StackInventoryEntry AddItem(string itemId, int quantity = 1) =>
        AddItem(_session.ContentRepository.GetItem(itemId), quantity);

    public StackInventoryEntry AddItem(ItemDefinition item, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(item);

        var entry = Inventory.AddItem(item, quantity);
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new ItemAcquiredEvent(item.Id, item.Name, quantity));
        return entry;
    }

    public void RemoveItem(string itemId, int quantity = 1) =>
        RemoveItem(_session.ContentRepository.GetItem(itemId), quantity);

    public void RemoveItem(ItemDefinition item, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(item);

        Inventory.RemoveItem(item, quantity);
        _session.Events.Publish(new InventoryChangedEvent());
    }

    public void EquipFromStack(string characterId, EquipmentDefinition equipmentDefinition) =>
        EquipFromStack(State.Party.GetMember(characterId), equipmentDefinition);

    public void EquipFromStack(CharacterInstance character, EquipmentDefinition equipmentDefinition)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(equipmentDefinition);

        Inventory.RemoveItem(equipmentDefinition);
        ReplaceEquippedItem(character, equipmentDefinition.SlotType);
        character.AddEquipmentInstance(EquipmentInstanceFactory.Create(equipmentDefinition));
        character.RebuildSnapshot();
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public void EquipInstance(string characterId, string equipmentInstanceId) =>
        EquipInstance(State.Party.GetMember(characterId), equipmentInstanceId);

    public void EquipInstance(CharacterInstance character, string equipmentInstanceId)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentException.ThrowIfNullOrWhiteSpace(equipmentInstanceId);

        var entry = Inventory.GetEquipmentInstanceEntry(equipmentInstanceId);

        var equipment = Inventory.RemoveEquipmentInstance(equipmentInstanceId);
        ReplaceEquippedItem(character, equipment.Definition.SlotType);
        character.AddEquipmentInstance(equipment);
        character.RebuildSnapshot();
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
    }

    public EquipmentInstance UnequipToInventory(string characterId, EquipmentSlotType slotType) =>
        UnequipToInventory(State.Party.GetMember(characterId), slotType);

    public EquipmentInstance UnequipToInventory(CharacterInstance character, EquipmentSlotType slotType)
    {
        ArgumentNullException.ThrowIfNull(character);

        var equipment = character.RemoveEquipment(slotType);
        character.RebuildSnapshot();
        Inventory.AddEquipmentInstance(equipment);
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new CharacterChangedEvent(character.Id));
        return equipment;
    }

    private void ReplaceEquippedItem(CharacterInstance character, EquipmentSlotType slotType)
    {
        if (character.GetEquipment(slotType) is null)
        {
            return;
        }

        Inventory.AddEquipmentInstance(character.RemoveEquipment(slotType));
    }
}
