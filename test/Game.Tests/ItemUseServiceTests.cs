using Game.Application;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Tests;

public sealed class ItemUseServiceTests
{
    [Fact]
    public void Use_ExternalSkillBook_LearnsDefaultLevel20AndDoesNotConsume()
    {
        var skill = TestContentFactory.CreateExternalSkill("dragon_palm");
        var book = CreateItem(
            "dragon_book",
            ItemType.SkillBook,
            [new GrantExternalSkillItemUseEffectDefinition(skill.Id)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [skill],
            items: [book]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(book);

        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(20, hero.GetExternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void Use_ExternalSkillBook_UpgradesKnownSkillBelowMaxAndDoesNotConsume()
    {
        var skill = TestContentFactory.CreateExternalSkill("dragon_palm");
        var book = CreateItem(
            "dragon_book",
            ItemType.SkillBook,
            [new GrantExternalSkillItemUseEffectDefinition(skill.Id)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 10)]);
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [skill],
            items: [book]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(book);

        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(20, hero.GetExternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void AnalyzeTarget_DisablesKnownExternalSkillAtMaxLevel()
    {
        var skill = TestContentFactory.CreateExternalSkill("dragon_palm");
        var book = CreateItem(
            "dragon_book",
            ItemType.SkillBook,
            [new GrantExternalSkillItemUseEffectDefinition(skill.Id)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 20)]);
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [skill],
            items: [book]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(book);

        var candidate = session.ItemUseService.AnalyzeTarget(entry, hero);

        Assert.False(candidate.CanUse);
        Assert.Equal("该外功已达上限", candidate.Reason);
    }

    [Fact]
    public void Use_InternalSkillBook_LearnsDefaultLevel20AndDoesNotConsume()
    {
        var skill = TestContentFactory.CreateInternalSkill("yijinjing");
        var book = CreateItem(
            "internal_book",
            ItemType.SkillBook,
            [new GrantInternalSkillItemUseEffectDefinition(skill.Id)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            internalSkills: [skill],
            items: [book]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(book);

        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(20, hero.GetInternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void Use_SpecialSkillBook_LearnsAndDoesNotConsume()
    {
        var skill = new SpecialSkillDefinition(
            "six_pulse",
            "six_pulse",
            "",
            "",
            0,
            new SkillCostDefinition(),
            null,
            "",
            "",
            null,
            []);
        var book = CreateItem(
            "special_book",
            ItemType.SpecialSkillBook,
            [new GrantSpecialSkillItemUseEffectDefinition(skill.Id)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            specialSkills: [skill],
            items: [book]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(book);

        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Contains(hero.GetSpecialSkills(), learned => learned.Definition.Id == skill.Id);
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void Use_TalentBook_LearnsAndConsumesOne()
    {
        var talent = new TalentDefinition { Id = "iron_body", Name = "iron_body" };
        var book = CreateItem(
            "talent_book",
            ItemType.TalentBook,
            [new GrantTalentItemUseEffectDefinition(talent.Id)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book, 2);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            talents: [talent],
            items: [book]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(book);

        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.True(result.Success);
        Assert.True(hero.HasTalent(talent.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void Use_Booster_IncreasesMaxStatsAndConsumesOne()
    {
        var booster = CreateItem(
            "peach",
            ItemType.Booster,
            [
                new AddMaxHpItemUseEffectDefinition(100),
                new AddMaxMpItemUseEffectDefinition(50),
            ]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.MaxHp] = 200,
                [StatType.MaxMp] = 80,
            });
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(booster);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            items: [booster]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(booster);

        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(300, hero.GetBaseStat(StatType.MaxHp));
        Assert.Equal(130, hero.GetBaseStat(StatType.MaxMp));
        Assert.Empty(state.Inventory.Entries);
    }

    [Fact]
    public void Use_Equipment_ReplacesOccupiedSlotAndReturnsOldEquipmentToInventory()
    {
        var oldSword = TestContentFactory.CreateEquipment("old_sword");
        var newSword = TestContentFactory.CreateEquipment("new_sword");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero", equipment: [oldSword]);
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(newSword);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            equipment: [oldSword, newSword]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(newSword);

        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(newSword.Id, hero.GetEquipment(EquipmentSlotType.Weapon)?.Definition.Id);
        var returned = Assert.IsType<StackInventoryEntry>(Assert.Single(state.Inventory.Entries));
        Assert.Equal(oldSword.Id, returned.Definition.Id);
        Assert.Equal(1, returned.Quantity);
    }

    private static GameState CreateStateWithHero(
        CharacterDefinition heroDefinition,
        out Game.Core.Model.Character.CharacterInstance hero)
    {
        var state = new GameState();
        hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        return state;
    }

    private static NormalItemDefinition CreateItem(
        string id,
        ItemType type,
        IReadOnlyList<ItemUseEffectDefinition> effects) =>
        new()
        {
            Id = id,
            Name = id,
            Type = type,
            UseEffects = effects,
        };
}
