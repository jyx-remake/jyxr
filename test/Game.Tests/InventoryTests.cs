using Game.Application;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Persistence;
using Game.Core.Serialization;
using System.Text.Json;

namespace Game.Tests;

public sealed class InventoryTests
{
    [Fact]
    public void AddItem_StacksNormalItemsAndKeepsOriginalEntryOrder()
    {
        var potion = CreateNormalItem("healing_potion");
        var antidote = CreateNormalItem("antidote");
        var inventory = new Inventory();

        inventory.AddItem(potion, 2);
        inventory.AddItem(antidote, 1);
        inventory.AddItem(potion, 3);

        Assert.Equal(["healing_potion", "antidote"], inventory.Entries.Select(entry => entry.Definition.Id).ToArray());
        var potionStack = Assert.IsType<StackInventoryEntry>(inventory.Entries[0]);
        Assert.Same(potion, potionStack.Definition);
        Assert.Equal(5, potionStack.Quantity);
        Assert.Equal(1, potionStack.EntryNumber);
    }

    [Fact]
    public void AddEquipmentInstance_StacksPlainEquipmentAndSeparatesAffixedEquipment()
    {
        var sword = TestContentFactory.CreateEquipment("iron_sword");
        var inventory = new Inventory();
        var equipmentInstanceFactory = new EquipmentInstanceFactory();
        var extraAffix = new StatModifierAffix(StatType.Bili, ModifierValue.Add(3));

        inventory.AddEquipmentInstance(equipmentInstanceFactory.Create(sword));
        inventory.AddEquipmentInstance(equipmentInstanceFactory.Create(sword));
        inventory.AddEquipmentInstance(equipmentInstanceFactory.Create(sword, [extraAffix]));

        Assert.Equal(2, inventory.Entries.Count);
        var swordStack = Assert.IsType<StackInventoryEntry>(inventory.Entries[0]);
        Assert.True(swordStack.IsEquipmentStack);
        Assert.Equal(2, swordStack.Quantity);

        var equipmentEntry = Assert.IsType<EquipmentInstanceInventoryEntry>(inventory.Entries[1]);
        Assert.Equal("iron_sword_00000003", equipmentEntry.Equipment.Id);
        Assert.Equal([extraAffix], equipmentEntry.Equipment.ExtraAffixes);
    }

    [Fact]
    public void RemoveItemAndAddAffixedEquipment_DecrementsStackAndAppendsIndependentEntry()
    {
        var sword = TestContentFactory.CreateEquipment("iron_sword");
        var charm = TestContentFactory.CreateEquipment("ward_charm", EquipmentSlotType.Accessory);
        var inventory = new Inventory();
        var equipmentInstanceFactory = new EquipmentInstanceFactory();
        var extraAffix = new StatModifierAffix(StatType.Gengu, ModifierValue.Add(2));

        inventory.AddItem(sword, 2);
        inventory.AddItem(charm, 1);
        inventory.RemoveItem(sword);
        var entry = Assert.IsType<EquipmentInstanceInventoryEntry>(
            inventory.AddEquipmentInstance(equipmentInstanceFactory.Create(sword, [extraAffix])));

        Assert.Equal(["iron_sword", "ward_charm", "iron_sword"], inventory.Entries.Select(item => item.Definition.Id).ToArray());
        Assert.Equal(1, inventory.GetStack(sword).Quantity);
        Assert.Same(entry, inventory.Entries[2]);
        Assert.Equal(3, entry.EntryNumber);
    }

    [Fact]
    public void InventoryService_EquipsFromStackAndUnequipsPlainEquipmentBackIntoStack()
    {
        var sword = TestContentFactory.CreateEquipment("iron_sword");
        var equipmentInstanceFactory = new EquipmentInstanceFactory();
        var character = TestContentFactory.CreateCharacterInstance("char_001", TestContentFactory.CreateCharacterDefinition("hero"), equipmentInstanceFactory);
        var party = new Party();
        party.AddMember(character);
        var inventory = new Inventory();
        inventory.AddItem(sword, 1);
        var repository = TestContentFactory.CreateRepository();
        var state = new GameState();
        state.SetParty(party);
        state.SetInventory(inventory);
        state.SetEquipmentInstanceFactory(equipmentInstanceFactory);
        var session = new GameSession(state, repository);

        session.InventoryService.EquipFromStack(character, sword);
        var equipped = character.GetEquipment(EquipmentSlotType.Weapon);
        Assert.NotNull(equipped);
        Assert.Empty(inventory.Entries);

        var unequipped = session.InventoryService.UnequipToInventory(character, EquipmentSlotType.Weapon);

        Assert.Same(equipped, unequipped);
        Assert.Null(character.GetEquipment(EquipmentSlotType.Weapon));
        var stack = Assert.IsType<StackInventoryEntry>(Assert.Single(inventory.Entries));
        Assert.Equal("iron_sword", stack.Definition.Id);
        Assert.Equal(1, stack.Quantity);
    }

    [Fact]
    public void InventoryService_EquipsIndependentInstanceAndUnequipsAffixedEquipmentAsNewEntry()
    {
        var sword = TestContentFactory.CreateEquipment("iron_sword");
        var equipmentInstanceFactory = new EquipmentInstanceFactory();
        var character = TestContentFactory.CreateCharacterInstance("char_001", TestContentFactory.CreateCharacterDefinition("hero"), equipmentInstanceFactory);
        var party = new Party();
        party.AddMember(character);
        var inventory = new Inventory();
        var extraAffix = new StatModifierAffix(StatType.Bili, ModifierValue.Add(3));
        var equipmentEntry = Assert.IsType<EquipmentInstanceInventoryEntry>(
            inventory.AddEquipmentInstance(equipmentInstanceFactory.Create(sword, [extraAffix])));
        var repository = TestContentFactory.CreateRepository();
        var state = new GameState();
        state.SetParty(party);
        state.SetInventory(inventory);
        state.SetEquipmentInstanceFactory(equipmentInstanceFactory);
        var session = new GameSession(state, repository);

        session.InventoryService.EquipInstance(character, equipmentEntry.Equipment.Id);
        Assert.Empty(inventory.Entries);

        session.InventoryService.UnequipToInventory(character, EquipmentSlotType.Weapon);

        var restoredEntry = Assert.IsType<EquipmentInstanceInventoryEntry>(Assert.Single(inventory.Entries));
        Assert.Equal(equipmentEntry.Equipment.Id, restoredEntry.Equipment.Id);
        Assert.Equal([extraAffix], restoredEntry.Equipment.ExtraAffixes);
    }

    [Fact]
    public void RebuildSnapshot_AppliesDefinitionAndInstanceAffixes()
    {
        var definitionAffix = new StatModifierAffix(StatType.Bili, ModifierValue.Add(2));
        var extraAffix = new StatModifierAffix(StatType.Bili, ModifierValue.Add(3));
        var sword = TestContentFactory.CreateEquipment("iron_sword") with
        {
            Affixes = [definitionAffix],
        };
        var equipmentInstanceFactory = new EquipmentInstanceFactory();
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001",
            TestContentFactory.CreateCharacterDefinition(
                "hero",
                new Dictionary<StatType, int>
                {
                    [StatType.Bili] = 10,
                }),
            equipmentInstanceFactory);

        character.AddEquipmentInstance(equipmentInstanceFactory.Create(sword, [extraAffix]));
        character.RebuildSnapshot();

        Assert.Equal(15, character.GetStat(StatType.Bili));
    }

    [Fact]
    public void Inventory_RoundTripsEntries()
    {
        var potion = CreateNormalItem("healing_potion");
        var sword = TestContentFactory.CreateEquipment("iron_sword");
        var extraAffix = new StatModifierAffix(StatType.Bili, ModifierValue.Add(3));
        var equipmentInstanceFactory = new EquipmentInstanceFactory();
        var inventory = new Inventory();
        inventory.AddItem(potion, 3);
        inventory.AddItem(sword, 2);
        inventory.AddEquipmentInstance(equipmentInstanceFactory.Create(sword, [extraAffix]));
        var repository = TestContentFactory.CreateRepository(
            items: [potion],
            equipment: [sword]);

        var record = InventoryMapper.ToRecord(inventory);
        var restored = InventoryMapper.FromRecord(record, repository);

        Assert.Equal(inventory.NextEntryNumber, restored.NextEntryNumber);
        Assert.Equal(["healing_potion", "iron_sword", "iron_sword"], restored.Entries.Select(entry => entry.Definition.Id).ToArray());
        Assert.Equal([1L, 2L, 3L], restored.Entries.Select(entry => entry.EntryNumber).ToArray());
        Assert.Equal(3, Assert.IsType<StackInventoryEntry>(restored.Entries[0]).Quantity);
        Assert.Equal(2, Assert.IsType<StackInventoryEntry>(restored.Entries[1]).Quantity);
        var equipment = Assert.IsType<EquipmentInstanceInventoryEntry>(restored.Entries[2]);
        Assert.Equal("iron_sword_00000001", equipment.Equipment.Id);
        Assert.Equal([extraAffix], equipment.Equipment.ExtraAffixes);
    }

    [Fact]
    public void EquipmentInstanceFactory_UsesDefinitionIdAndGlobalSequence()
    {
        var sword = TestContentFactory.CreateEquipment("iron_sword");
        var charm = TestContentFactory.CreateEquipment("ward_charm", EquipmentSlotType.Accessory);
        var factory = new EquipmentInstanceFactory();

        var first = factory.Create(sword);
        var second = factory.Create(charm);

        Assert.Equal("iron_sword_00000001", first.Id);
        Assert.Equal("ward_charm_00000002", second.Id);
        Assert.Equal(3, factory.NextNumber);
    }

    [Fact]
    public void SaveGame_RoundTripsInventoryAndEquippedExtraAffixes()
    {
        var potion = CreateNormalItem("healing_potion");
        var sword = TestContentFactory.CreateEquipment("iron_sword");
        var extraAffix = new StatModifierAffix(StatType.Bili, ModifierValue.Add(3));
        var equipmentInstanceFactory = new EquipmentInstanceFactory();
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition, equipmentInstanceFactory);
        character.AddEquipmentInstance(equipmentInstanceFactory.Create(sword, [extraAffix]));
        var party = new Party();
        party.AddMember(character);
        var inventory = new Inventory();
        inventory.AddItem(potion, 2);
        inventory.AddItem(sword, 1);
        var repository = TestContentFactory.CreateRepository(
            characters: [definition],
            items: [potion],
            equipment: [sword]);

        var saveGame = SaveGame.Create(new AdventureState(), party, inventory, new ChestState(), equipmentInstanceFactory, new CurrencyState(), new ClockState(), new LocationState(), new MapEventProgressState());
        var restoredCharacters = saveGame.RestoreCharacters(repository);
        var restoredInventory = saveGame.RestoreInventory(repository);
        var restoredEquipmentInstanceFactory = saveGame.RestoreEquipmentInstanceFactory();

        Assert.Equal(SaveGame.CurrentVersion, saveGame.Version);
        var restoredEquipment = restoredCharacters["char_001"].GetEquipment(EquipmentSlotType.Weapon);
        Assert.NotNull(restoredEquipment);
        Assert.Equal([extraAffix], restoredEquipment!.ExtraAffixes);
        Assert.Equal(["healing_potion", "iron_sword"], restoredInventory.Entries.Select(entry => entry.Definition.Id).ToArray());
        Assert.Equal(equipmentInstanceFactory.NextNumber, restoredEquipmentInstanceFactory.NextNumber);
    }

    [Fact]
    public void SaveGame_SerializesInventoryEntryDiscriminatorsAndExtraAffixes()
    {
        var potion = CreateNormalItem("healing_potion");
        var sword = TestContentFactory.CreateEquipment("iron_sword");
        var extraAffix = new StatModifierAffix(StatType.Bili, ModifierValue.Add(3));
        var equipmentInstanceFactory = new EquipmentInstanceFactory();
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition, equipmentInstanceFactory);
        var party = new Party();
        party.AddMember(character);
        var inventory = new Inventory();
        inventory.AddItem(potion, 2);
        inventory.AddEquipmentInstance(equipmentInstanceFactory.Create(sword, [extraAffix]));
        var repository = TestContentFactory.CreateRepository(
            characters: [definition],
            items: [potion],
            equipment: [sword]);

        var json = JsonSerializer.Serialize(SaveGame.Create(new AdventureState(), party, inventory, new ChestState(), equipmentInstanceFactory, new CurrencyState(), new ClockState(), new LocationState(), new MapEventProgressState()), GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<SaveGame>(json, GameJson.Default);
        Assert.NotNull(roundTripped);

        Assert.Contains("\"kind\":\"stack\"", json, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"equipment_instance\"", json, StringComparison.Ordinal);
        Assert.IsType<StackInventoryEntryRecord>(roundTripped!.Inventory.Entries[0]);
        Assert.IsType<EquipmentInstanceInventoryEntryRecord>(roundTripped.Inventory.Entries[1]);

        var restoredInventory = roundTripped.RestoreInventory(repository);
        var equipmentEntry = Assert.IsType<EquipmentInstanceInventoryEntry>(restoredInventory.Entries[1]);
        var restoredAffix = Assert.IsType<StatModifierAffix>(Assert.Single(equipmentEntry.Equipment.ExtraAffixes));
        Assert.Equal(StatType.Bili, restoredAffix.Stat);
    }

    private static NormalItemDefinition CreateNormalItem(string id) =>
        new()
        {
            Id = id,
            Name = id,
            Type = ItemType.Consumable,
        };
}
