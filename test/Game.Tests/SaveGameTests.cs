using System.Text.Json;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Serialization;
using Game.Content.Loading;
using Game.Core.Model.Character;
using Game.Core.Story;

namespace Game.Tests;

public sealed class SaveGameTests
{
    [Fact]
    public void SaveGame_RoundTripsCharactersAndParties()
    {
        var slashForm = new FormSkillDefinition(
            "slash_form",
            "slash_form",
            "",
            "",
            1,
            0,
            new SkillCostDefinition(),
            new SkillTargetingDefinition(CastSize: 0, ImpactSize: 0),
            0d,
            "",
            "",
            []);
        var basicAttack = TestContentFactory.CreateExternalSkill("basic_attack", formSkills: [slashForm]);
        var guardShout = TestContentFactory.CreateExternalSkill("guard_shout");
        var basicInternal = TestContentFactory.CreateInternalSkill("basic_internal");
        var bloodRush = new SpecialSkillDefinition(
            "blood_rush",
            "blood_rush",
            "",
            "",
            0,
            new SkillCostDefinition(),
            new SkillTargetingDefinition(),
            "",
            "",
            null,
            []);
        var ironBlade = TestContentFactory.CreateEquipment("iron_blade", EquipmentSlotType.Weapon);
        var wardCharm = TestContentFactory.CreateEquipment("ward_charm", EquipmentSlotType.Accessory);
        var healingPotion = new NormalItemDefinition
        {
            Id = "healing_potion",
            Name = "Healing Potion",
            Type = ItemType.Consumable,
        };
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_knight",
            externalSkills: [new InitialExternalSkillEntryDefinition(basicAttack)],
            internalSkills: [new InitialInternalSkillEntryDefinition(basicInternal, Equipped: true)],
            specialSkills: [bloodRush],
            equipment: [ironBlade]);

        var repository = TestContentFactory.CreateRepository(
            characters: [definition],
            externalSkills: [basicAttack, guardShout],
            internalSkills: [basicInternal],
            specialSkills: [bloodRush],
            talents:
            [
                new TalentDefinition
                {
                    Id = "battle_focus",
                    Name = "battle_focus",
                },
            ],
            equipment: [ironBlade, wardCharm],
            items: [healingPotion]);

        var inventory = new Inventory();
        var equipmentInstanceFactory = new EquipmentInstanceFactory();
        var first = TestContentFactory.CreateCharacterInstance("char_001", definition, equipmentInstanceFactory);
        first.Name = "Knight Alpha";
        first.Portrait = "portrait.knight_alpha";
        first.Model = "model.knight_alpha";
        first.GrantExperience(120);
        first.GrantStatPoints(2);
        first.AllocateStat(StatType.Bili, 2);
        first.SetExternalSkillState(guardShout, 4, 20, false);
        first.AddEquipmentInstance(new EquipmentInstance("equip_bonus_001", repository.GetEquipment("ward_charm")));

        var second = TestContentFactory.CreateCharacterInstance("char_002", definition, equipmentInstanceFactory);
        second.Name = "Knight Beta";
        second.UnlockedTalents.Add(repository.GetTalent("battle_focus"));

        var party = new Party();
        party.AddMember(first);
        party.AddMember(second);

        inventory.AddItem(repository.GetItem("iron_blade"), 2);
        inventory.AddItem(healingPotion, 3);

        var saveGame = SaveGame.Create(new AdventureState(), party, inventory, new ChestState(), equipmentInstanceFactory, new CurrencyState(), new ClockState(), new LocationState(), new MapEventProgressState(), new WorldTriggerState());
        var restoredCharacters = saveGame.RestoreCharacters(repository);
        var restoredParty = saveGame.RestoreParty(restoredCharacters);
        var restoredInventory = saveGame.RestoreInventory(repository);

        Assert.Equal(SaveGame.CurrentVersion, saveGame.Version);
        Assert.Equal(["char_001", "char_002"], restoredCharacters.Keys.OrderBy(key => key).ToArray());

        var restoredFirst = restoredCharacters["char_001"];
        Assert.Equal("Knight Alpha", restoredFirst.Name);
        Assert.Equal("portrait.knight_alpha", restoredFirst.Portrait);
        Assert.Equal("model.knight_alpha", restoredFirst.Model);
        Assert.Equal(first.GrowTemplateId, restoredFirst.GrowTemplateId);
        Assert.Equal(120, restoredFirst.Experience);
        Assert.Equal(0, restoredFirst.UnspentStatPoints);
        Assert.Equal(2, restoredFirst.BaseStats[StatType.Bili]);
        Assert.Contains(restoredFirst.ExternalSkills, skill => skill.Definition.Id == "guard_shout" && skill.Level == 4 && skill.Exp == 20 && !skill.IsActive);
        Assert.Contains(restoredFirst.InternalSkills, skill => skill.Definition.Id == "basic_internal" && skill.IsEquipped);
        Assert.Contains(restoredFirst.SpecialSkills, skill => skill.Definition.Id == "blood_rush" && skill.IsActive);
        Assert.Equal("equip_bonus_001", restoredFirst.EquippedItems[EquipmentSlotType.Accessory].Id);

        Assert.Equal(["char_001", "char_002"], restoredParty.Members.Select(member => member.Id).ToArray());
        Assert.Equal([1L, 2L], restoredInventory.Entries.Select(entry => entry.EntryNumber).ToArray());
    }

    [Fact]
    public void RestoreCharacters_IgnoresDefinitionSkillMaxLevels()
    {
        var externalSkill = TestContentFactory.CreateExternalSkill("blade");
        var internalSkill = TestContentFactory.CreateInternalSkill("inner");
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(externalSkill, MaxLevel: 8)],
            internalSkills: [new InitialInternalSkillEntryDefinition(internalSkill, MaxLevel: 9)]);
        var repository = TestContentFactory.CreateRepository(
            characters: [definition],
            externalSkills: [externalSkill],
            internalSkills: [internalSkill]);
        var character = TestContentFactory.CreateCharacterInstance("hero", definition, new EquipmentInstanceFactory());

        var saveGame = SaveGame.Create(
            new AdventureState(),
            CreateReserveParty(character),
            new Inventory(),
            new ChestState(),
            new EquipmentInstanceFactory(),
            new CurrencyState(),
            new ClockState(),
            new LocationState(),
            new MapEventProgressState(),
            new WorldTriggerState());

        var restored = saveGame.RestoreCharacters(repository)["hero"];

        Assert.Equal(20, restored.GetExternalSkills().Single().MaxLevel);
        Assert.Equal(20, restored.GetInternalSkills().Single().MaxLevel);
    }

    [Fact]
    public void SaveGame_RoundTripsSpecialBattleState()
    {
        var specialBattle = new SpecialBattleState();
        specialBattle.MarkTrialCompleted("阿青");
        specialBattle.AddTowerRewardClaim("倚天剑");
        specialBattle.AddTowerRewardClaim("倚天剑");

        var saveGame = SaveGame.Create(
            new AdventureState(),
            new Party(),
            new Inventory(),
            new ChestState(),
            new EquipmentInstanceFactory(),
            new CurrencyState(),
            new ClockState(),
            new LocationState(),
            new MapEventProgressState(),
            new WorldTriggerState(),
            specialBattleState: specialBattle);

        var restored = saveGame.RestoreSpecialBattleState();

        Assert.True(restored.IsTrialCompleted("阿青"));
        Assert.Equal(2, restored.GetTowerRewardClaimCount("倚天剑"));
    }

    [Fact]
    public void SaveGame_SerializesBaseStatKeysUsingStableEnumNames()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero_knight");

        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        character.GrantStatPoints(2);
        character.AllocateStat(StatType.Bili, 2);

        var saveGame = SaveGame.Create(new AdventureState(), CreateReserveParty(character), new Inventory(), new ChestState(), new EquipmentInstanceFactory(), new CurrencyState(), new ClockState(), new LocationState(), new MapEventProgressState(), new WorldTriggerState());
        var json = JsonSerializer.Serialize(saveGame, GameJson.Default);

        Assert.Contains("\"bili\":2", json, StringComparison.Ordinal);

        var roundTripped = JsonSerializer.Deserialize<SaveGame>(json, GameJson.Default);
        Assert.NotNull(roundTripped);
        Assert.Equal(2, roundTripped!.Characters[0].BaseStats[StatType.Bili]);
    }

    [Fact]
    public void CurrencyState_AddsSpendsAndRejectsInvalidAmounts()
    {
        var currency = new CurrencyState();

        currency.AddSilver(120);
        currency.AddGold(7);
        currency.SpendSilver(45);
        currency.SpendGold(2);

        Assert.Equal(75, currency.Silver);
        Assert.Equal(5, currency.Gold);
        Assert.True(currency.CanSpendSilver(75));
        Assert.True(currency.CanSpendGold(5));
        Assert.False(currency.CanSpendSilver(76));
        Assert.False(currency.CanSpendGold(6));
        Assert.Throws<InvalidOperationException>(() => currency.SpendSilver(76));
        Assert.Throws<InvalidOperationException>(() => currency.SpendGold(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => currency.AddSilver(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => currency.AddGold(-1));
    }

    [Fact]
    public void CurrencyState_ChangesCurrencyBySignedDelta()
    {
        var currency = new CurrencyState();

        currency.ChangeSilver(100);
        currency.ChangeGold(8);
        currency.ChangeSilver(-35);
        currency.ChangeGold(-3);
        currency.ChangeSilver(0);
        currency.ChangeGold(0);

        Assert.Equal(65, currency.Silver);
        Assert.Equal(5, currency.Gold);
        Assert.Throws<InvalidOperationException>(() => currency.ChangeSilver(-66));
        Assert.Throws<InvalidOperationException>(() => currency.ChangeGold(-6));
        Assert.Throws<ArgumentOutOfRangeException>(() => currency.ChangeSilver(int.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => currency.ChangeGold(int.MinValue));
    }

    [Fact]
    public void SaveGame_RoundTripsCurrency()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero_knight");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var repository = TestContentFactory.CreateRepository(characters: [definition]);
        var currency = new CurrencyState();
        currency.AddSilver(320);
        currency.AddGold(12);

        var saveGame = SaveGame.Create(new AdventureState(), CreateReserveParty(character), new Inventory(), new ChestState(), new EquipmentInstanceFactory(), currency, new ClockState(), new LocationState(), new MapEventProgressState(), new WorldTriggerState());
        var json = JsonSerializer.Serialize(saveGame, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<SaveGame>(json, GameJson.Default);

        Assert.NotNull(roundTripped);
        Assert.Contains("\"Silver\":320", json, StringComparison.Ordinal);
        Assert.Contains("\"Gold\":12", json, StringComparison.Ordinal);
        Assert.Empty(roundTripped!.RestoreCharacters(repository).Values.Single().EquippedItems);
        var restoredCurrency = roundTripped.RestoreCurrency();
        Assert.Equal(320, restoredCurrency.Silver);
        Assert.Equal(12, restoredCurrency.Gold);
    }

    [Fact]
    public void SaveGame_RoundTripsClock()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero_knight");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var clock = new ClockState();
        clock.AdvanceTimeSlots(10);
        clock.AdvanceDays(397);

        var saveGame = SaveGame.Create(new AdventureState(), CreateReserveParty(character), new Inventory(), new ChestState(), new EquipmentInstanceFactory(), new CurrencyState(), clock, new LocationState(), new MapEventProgressState(), new WorldTriggerState());
        var json = JsonSerializer.Serialize(saveGame, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<SaveGame>(json, GameJson.Default);

        Assert.NotNull(roundTripped);
        Assert.Contains("\"Year\":2", json, StringComparison.Ordinal);
        Assert.Contains("\"Month\":2", json, StringComparison.Ordinal);
        Assert.Contains("\"Day\":9", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"TotalDays\"", json, StringComparison.Ordinal);
        Assert.Contains("\"TimeSlot\":\"Yin\"", json, StringComparison.Ordinal);
        var restoredClock = roundTripped!.RestoreClock();
        Assert.Equal(2, restoredClock.Year);
        Assert.Equal(2, restoredClock.Month);
        Assert.Equal(9, restoredClock.Day);
        Assert.Equal(398, restoredClock.TotalDays);
        Assert.Equal(TimeSlot.Yin, restoredClock.TimeSlot);
    }

    [Fact]
    public void SaveGame_RoundTripsLocation()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero_knight");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var location = new LocationState();
        location.ChangeMap("nanxian_house");
        location.SetLargeMapPosition("world", new MapPosition(512, 410));
        location.SetLargeMapPosition("jiangnan", new MapPosition(24, 36));

        var saveGame = SaveGame.Create(new AdventureState(), CreateReserveParty(character), new Inventory(), new ChestState(), new EquipmentInstanceFactory(), new CurrencyState(), new ClockState(), location, new MapEventProgressState(), new WorldTriggerState());
        var json = JsonSerializer.Serialize(saveGame, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<SaveGame>(json, GameJson.Default);

        Assert.NotNull(roundTripped);
        Assert.Contains("\"CurrentMapId\":\"nanxian_house\"", json, StringComparison.Ordinal);
        Assert.Contains("\"LargeMapPositions\"", json, StringComparison.Ordinal);
        Assert.Contains("\"world\"", json, StringComparison.Ordinal);
        Assert.Contains("\"jiangnan\"", json, StringComparison.Ordinal);
        Assert.Contains("\"X\":512", json, StringComparison.Ordinal);
        Assert.Contains("\"Y\":410", json, StringComparison.Ordinal);
        var restoredLocation = roundTripped!.RestoreLocation();
        Assert.Equal("nanxian_house", restoredLocation.CurrentMapId);
        Assert.Equal(new MapPosition(512, 410), restoredLocation.GetLargeMapPosition("world"));
        Assert.Equal(new MapPosition(24, 36), restoredLocation.GetLargeMapPosition("jiangnan"));
        Assert.Equal(MapPosition.Zero, restoredLocation.GetLargeMapPosition("unknown"));
    }

    [Fact]
    public void SaveGame_RoundTripsMapEventProgress()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero_knight");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var mapEventProgress = new MapEventProgressState();
        mapEventProgress.MarkCompleted("world|village|0");
        mapEventProgress.MarkCompleted("world|sect_gate|1");

        var saveGame = SaveGame.Create(
            new AdventureState(),
            CreateReserveParty(character),
            new Inventory(),
            new ChestState(),
            new EquipmentInstanceFactory(),
            new CurrencyState(),
            new ClockState(),
            new LocationState(),
            mapEventProgress,
            new WorldTriggerState());
        var json = JsonSerializer.Serialize(saveGame, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<SaveGame>(json, GameJson.Default);

        Assert.NotNull(roundTripped);
        Assert.Contains("\"MapEventProgress\"", json, StringComparison.Ordinal);
        Assert.Contains("world|village|0", json, StringComparison.Ordinal);
        var restoredProgress = roundTripped!.RestoreMapEventProgress();
        Assert.True(restoredProgress.IsCompleted("world|village|0"));
        Assert.True(restoredProgress.IsCompleted("world|sect_gate|1"));
        Assert.False(restoredProgress.IsCompleted("world|unknown|0"));
    }

    [Fact]
    public void SaveGame_RoundTripsStoryState()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero_knight");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var story = new StoryState();
        story.SetVariable("hero_name", ExprValue.FromString("张无忌"));
        story.SetVariable("met_nanxian", ExprValue.FromBoolean(true));
        story.SetVariable("boss_level", ExprValue.FromNumber(12));
        story.MarkCompleted("新手村_南贤开场");
        story.MarkCompleted("新手村_南贤");
        story.SetLastStory("新手村_南贤");
        var clock = new ClockState();
        story.SetTimeKey("襄阳急报", clock, 5, "襄阳_超时");

        var saveGame = SaveGame.Create(
            new AdventureState(),
            CreateReserveParty(character),
            new Inventory(),
            new ChestState(),
            new EquipmentInstanceFactory(),
            new CurrencyState(),
            clock,
            new LocationState(),
            new MapEventProgressState(),
            new WorldTriggerState(),
            storyState: story);
        var json = JsonSerializer.Serialize(saveGame, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<SaveGame>(json, GameJson.Default);

        Assert.NotNull(roundTripped);
        Assert.Contains("\"StoryState\"", json, StringComparison.Ordinal);
        Assert.Contains("\"LastStoryId\"", json, StringComparison.Ordinal);
        var restoredStory = roundTripped!.RestoreStoryState();
        Assert.True(restoredStory.IsStoryCompleted("新手村_南贤开场"));
        Assert.True(restoredStory.IsStoryCompleted("新手村_南贤"));
        Assert.Equal("新手村_南贤", restoredStory.LastStoryId);
        Assert.True(restoredStory.TryGetVariable("hero_name", out var heroName));
        Assert.Equal(ExprValueKind.String, heroName.Kind);
        Assert.Equal("张无忌", heroName.Text);
        Assert.True(restoredStory.TryGetVariable("met_nanxian", out var metNanxian));
        Assert.True(metNanxian.Boolean);
        Assert.True(restoredStory.TryGetVariable("boss_level", out var bossLevel));
        Assert.Equal(12d, bossLevel.Number);
        var timeKey = Assert.Single(restoredStory.TimeKeys.Values);
        Assert.Equal("襄阳急报", timeKey.Key);
        Assert.Equal(5, timeKey.LimitDays);
        Assert.Equal("襄阳_超时", timeKey.TargetStoryId);
        Assert.Equal(6, timeKey.DeadlineAt.Day);
    }

    [Fact]
    public void SaveGame_RoundTripsJournal()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero_knight");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var journal = new JournalState();
        var clock = ClockState.Restore(new ClockRecord(2, 3, 4, TimeSlot.You));
        journal.Append(clock, "初入襄阳");
        clock.AdvanceDays(1);
        journal.Append(clock, "夜探敌营");

        var saveGame = SaveGame.Create(
            new AdventureState(),
            CreateReserveParty(character),
            new Inventory(),
            new ChestState(),
            new EquipmentInstanceFactory(),
            new CurrencyState(),
            new ClockState(),
            new LocationState(),
            new MapEventProgressState(),
            new WorldTriggerState(),
            storyState: new StoryState(),
            journal: journal);
        var json = JsonSerializer.Serialize(saveGame, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<SaveGame>(json, GameJson.Default);

        Assert.NotNull(roundTripped);
        Assert.Contains("\"Journal\"", json, StringComparison.Ordinal);
        Assert.Contains("初入襄阳", json, StringComparison.Ordinal);
        var restoredJournal = roundTripped!.RestoreJournal();
        Assert.Equal(2, restoredJournal.Entries.Count);
        Assert.Equal("初入襄阳", restoredJournal.Entries[0].Text);
        Assert.Equal(2, restoredJournal.Entries[0].Timestamp.Year);
        Assert.Equal(3, restoredJournal.Entries[0].Timestamp.Month);
        Assert.Equal(4, restoredJournal.Entries[0].Timestamp.Day);
        Assert.Equal(TimeSlot.You, restoredJournal.Entries[0].Timestamp.TimeSlot);
        Assert.Equal("夜探敌营", restoredJournal.Entries[1].Text);
    }

    [Fact]
    public void SaveGame_RoundTripsCharacterGrowTemplateOverride()
    {
        var defaultGrowth = TestContentFactory.CreateGrowTemplate("default", new Dictionary<StatType, int>());
        var wandererGrowth = TestContentFactory.CreateGrowTemplate("wanderer", new Dictionary<StatType, int>());
        var definition = TestContentFactory.CreateCharacterDefinition("hero_knight", growTemplate: "default");
        var repository = TestContentFactory.CreateRepository(
            characters: [definition],
            growTemplates: [defaultGrowth, wandererGrowth]);
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        character.GrowTemplateId = "wanderer";

        var saveGame = SaveGame.Create(
            new AdventureState(),
            CreateReserveParty(character),
            new Inventory(),
            new ChestState(),
            new EquipmentInstanceFactory(),
            new CurrencyState(),
            new ClockState(),
            new LocationState(),
            new MapEventProgressState(),
            new WorldTriggerState());

        var restored = saveGame.RestoreCharacters(repository)["char_001"];

        Assert.Equal("wanderer", restored.GrowTemplateId);
        Assert.Equal("default", restored.Definition.GrowTemplate);
    }

    private static Party CreateReserveParty(params CharacterInstance[] characters)
    {
        var party = new Party();
        foreach (var character in characters)
        {
            party.AddReserve(character);
        }

        return party;
    }
}
