using Game.Application;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Tests;

public sealed class EquipmentGrantedSkillsTests
{
    private static SpecialSkillDefinition CreateSpecialSkill(string id) => new(
        id,
        id,
        "",
        SpecialSkillIntent.Support,
        "",
        0,
        new SkillCostDefinition(0, 0),
        new SkillTargetingDefinition(),
        "",
        "",
        null,
        []);

    private static GameSession CreateSession(
        EquipmentDefinition equipment,
        ExternalSkillDefinition externalSkill,
        SpecialSkillDefinition specialSkill,
        out CharacterInstance character)
    {
        var repository = TestContentFactory.CreateRepository(
            externalSkills: [externalSkill],
            specialSkills: [specialSkill],
            equipment: [equipment]);
        character = TestContentFactory.CreateCharacterInstance(
            "char_001", TestContentFactory.CreateCharacterDefinition("hero"));
        var party = new Party();
        party.AddMember(character);
        var inventory = new Inventory();
        inventory.AddItem(equipment, 1);
        var state = new GameState();
        state.SetParty(party);
        state.SetInventory(inventory);
        state.SetEquipmentInstanceFactory(new EquipmentInstanceFactory());
        return new GameSession(state, repository);
    }

    private static EquipmentDefinition CreateGrantedEquipment() => new()
    {
        Id = "wudao_tianshu",
        Name = "wudao_tianshu",
        Type = ItemType.Equipment,
        ConsumeOnUse = false,
        SlotType = EquipmentSlotType.Accessory,
        GrantedSkills = [new EquipmentGrantedSkillDefinition("baijia_jianfa", 20)],
        GrantedSpecialSkills = [new EquipmentGrantedSpecialSkillDefinition("wanli_zhuiyun")],
    };

    [Fact]
    public void EquipGrantsSkillsAndUnequipRevokesThem()
    {
        var session = CreateSession(
            CreateGrantedEquipment(),
            TestContentFactory.CreateExternalSkill("baijia_jianfa"),
            CreateSpecialSkill("wanli_zhuiyun"),
            out var character);

        session.InventoryService.EquipFromStack(character, session.ContentRepository.GetEquipment("wudao_tianshu"));

        var external = Assert.Single(character.ExternalSkills);
        Assert.Equal("baijia_jianfa", external.Id);
        Assert.Equal(20, external.Level);
        Assert.True(external.IsActive);
        Assert.Equal("wanli_zhuiyun", Assert.Single(character.SpecialSkills).Id);

        session.InventoryService.UnequipToInventory(character, EquipmentSlotType.Accessory);

        Assert.Empty(character.ExternalSkills);
        Assert.Empty(character.SpecialSkills);
    }

    [Fact]
    public void UnequipKeepsBookLearnedSkill()
    {
        var session = CreateSession(
            CreateGrantedEquipment(),
            TestContentFactory.CreateExternalSkill("baijia_jianfa"),
            CreateSpecialSkill("wanli_zhuiyun"),
            out var character);
        character.SetExternalSkillState(
            session.ContentRepository.GetExternalSkill("baijia_jianfa"), 5, 0, true);

        session.InventoryService.EquipFromStack(character, session.ContentRepository.GetEquipment("wudao_tianshu"));
        session.InventoryService.UnequipToInventory(character, EquipmentSlotType.Accessory);

        var kept = Assert.Single(character.ExternalSkills);
        Assert.Equal(5, kept.Level);
    }

    [Fact]
    public void SaveLoadRoundtripPreservesGrantProvenance()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [TestContentFactory.CreateExternalSkill("baijia_jianfa")],
            specialSkills: [CreateSpecialSkill("wanli_zhuiyun")],
            equipment: [CreateGrantedEquipment()]);
        var character = TestContentFactory.CreateCharacterInstance("char_001", heroDefinition);
        var party = new Party();
        party.AddMember(character);
        var inventory = new Inventory();
        inventory.AddItem(repository.GetEquipment("wudao_tianshu"), 1);
        var state = new GameState();
        state.SetParty(party);
        state.SetInventory(inventory);
        state.SetEquipmentInstanceFactory(new EquipmentInstanceFactory());
        var session = new GameSession(state, repository);
        session.InventoryService.EquipFromStack(character, repository.GetEquipment("wudao_tianshu"));

        var restored = CharacterMapper.FromRecord(CharacterMapper.ToRecord(character), repository);
        var restoredParty = new Party();
        restoredParty.AddMember(restored);
        var restoredState = new GameState();
        restoredState.SetParty(restoredParty);
        restoredState.SetInventory(new Inventory());
        restoredState.SetEquipmentInstanceFactory(new EquipmentInstanceFactory());
        var restoredSession = new GameSession(restoredState, repository);

        restoredSession.InventoryService.RestoreEquipmentGrantedSkills();
        restoredSession.InventoryService.UnequipToInventory(restored, EquipmentSlotType.Accessory);

        Assert.Empty(restored.ExternalSkills);
        Assert.Empty(restored.SpecialSkills);
    }

    [Fact]
    public void OverlappingGearTransfersOwnershipOnUnequip()
    {
        var extra = TestContentFactory.CreateExternalSkill("baijia_jianfa");
        var special = CreateSpecialSkill("wanli_zhuiyun");
        var first = CreateGrantedEquipment();
        var second = new EquipmentDefinition
        {
            Id = "second_tome",
            Name = "second_tome",
            Type = ItemType.Equipment,
            ConsumeOnUse = false,
            SlotType = EquipmentSlotType.Weapon,
            GrantedSkills = [new EquipmentGrantedSkillDefinition("baijia_jianfa", 1)],
        };
        var repository = TestContentFactory.CreateRepository(
            externalSkills: [extra],
            specialSkills: [special],
            equipment: [first, second]);
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001", TestContentFactory.CreateCharacterDefinition("hero"));
        var party = new Party();
        party.AddMember(character);
        var inventory = new Inventory();
        inventory.AddItem(first, 1);
        inventory.AddItem(second, 1);
        var state = new GameState();
        state.SetParty(party);
        state.SetInventory(inventory);
        state.SetEquipmentInstanceFactory(new EquipmentInstanceFactory());
        var session = new GameSession(state, repository);

        session.InventoryService.EquipFromStack(character, first);
        session.InventoryService.EquipFromStack(character, second);
        session.InventoryService.UnequipToInventory(character, EquipmentSlotType.Accessory);

        Assert.Single(character.ExternalSkills);
        session.InventoryService.UnequipToInventory(character, EquipmentSlotType.Weapon);
        Assert.Empty(character.ExternalSkills);
    }

    [Fact]
    public void IsEquipmentGrantedSkillTracksLiveCoverage()
    {
        var session = CreateSession(
            CreateGrantedEquipment(),
            TestContentFactory.CreateExternalSkill("baijia_jianfa"),
            CreateSpecialSkill("wanli_zhuiyun"),
            out var character);

        Assert.False(character.IsEquipmentGrantedSkill(
            Game.Core.Model.Skills.SkillKind.External, "baijia_jianfa"));

        session.InventoryService.EquipFromStack(character, session.ContentRepository.GetEquipment("wudao_tianshu"));

        Assert.True(character.IsEquipmentGrantedSkill(
            Game.Core.Model.Skills.SkillKind.External, "baijia_jianfa"));
        Assert.True(character.IsEquipmentGrantedSkill(
            Game.Core.Model.Skills.SkillKind.Special, "wanli_zhuiyun"));

        session.InventoryService.UnequipToInventory(character, EquipmentSlotType.Accessory);

        Assert.False(character.IsEquipmentGrantedSkill(
            Game.Core.Model.Skills.SkillKind.External, "baijia_jianfa"));
    }
}
