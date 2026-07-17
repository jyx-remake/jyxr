using Game.Application;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Tests;

public sealed class ItemUseServiceTests
{
    [Fact]
    public void Use_ExternalSkillBook_LearnsDefaultLevel10AndDoesNotConsume()
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
        Assert.Equal(10, hero.GetExternalSkillLevel(skill.Id));
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
            externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 5)]);
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
        Assert.Equal(10, hero.GetExternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void Use_ExternalSkillBook_UpgradesKnownSkillWhenExternalSkillCountAtLimit()
    {
        var skill = TestContentFactory.CreateExternalSkill("dragon_palm");
        var book = CreateItem(
            "dragon_book",
            ItemType.SkillBook,
            [new GrantExternalSkillItemUseEffectDefinition(skill.Id)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 5)]);
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [skill],
            items: [book]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig { MaxExternalSkillCount = 1 });
        var entry = state.Inventory.GetStack(book);

        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(10, hero.GetExternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void Use_ExternalSkillBook_RespectsBookEffectLevelByDefault()
    {
        var skill = TestContentFactory.CreateExternalSkill("dragon_palm");
        var book = CreateItem(
            "dragon_book",
            ItemType.SkillBook,
            [new GrantExternalSkillItemUseEffectDefinition(skill.Id, Level: 5)]);
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
        Assert.Equal(5, hero.GetExternalSkillLevel(skill.Id));
    }

    [Fact]
    public void Use_ExternalSkillBook_IgnoresBookEffectLevelWhenConfigured()
    {
        var skill = TestContentFactory.CreateExternalSkill("dragon_palm");
        var book = CreateItem(
            "dragon_book",
            ItemType.SkillBook,
            [new GrantExternalSkillItemUseEffectDefinition(skill.Id, Level: 5)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [skill],
            items: [book]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig { IgnoreSkillBookLevelLimit = true });
        var entry = state.Inventory.GetStack(book);

        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(10, hero.GetExternalSkillLevel(skill.Id));
    }

    [Fact]
    public void Use_ExternalSkillBook_ClampsBookEffectLevelToCurrentMaxLevel()
    {
        var skill = TestContentFactory.CreateExternalSkill("dragon_palm");
        var book = CreateItem(
            "dragon_book",
            ItemType.SkillBook,
            [new GrantExternalSkillItemUseEffectDefinition(skill.Id, Level: 15)]);
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
        Assert.Equal(10, hero.GetExternalSkillLevel(skill.Id));
    }

    [Fact]
    public void AnalyzeTarget_DisablesNewExternalSkillWhenExternalSkillCountAtLimit()
    {
        var knownSkill = TestContentFactory.CreateExternalSkill("known_palm");
        var newSkill = TestContentFactory.CreateExternalSkill("dragon_palm");
        var book = CreateItem(
            "dragon_book",
            ItemType.SkillBook,
            [new GrantExternalSkillItemUseEffectDefinition(newSkill.Id)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(knownSkill)]);
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [knownSkill, newSkill],
            items: [book]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig { MaxExternalSkillCount = 1 });
        var entry = state.Inventory.GetStack(book);

        var candidate = session.ItemUseService.AnalyzeTarget(entry, hero);
        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.False(candidate.CanUse);
        Assert.Equal("外功数量已达上限", candidate.Reason);
        Assert.False(result.Success);
        Assert.Equal("外功数量已达上限", result.Message);
        Assert.Null(hero.GetExternalSkillLevel(newSkill.Id));
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
            externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 10)]);
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
    public void AnalyzeTarget_DisablesKnownExternalSkillAtBookEffectLevel()
    {
        var skill = TestContentFactory.CreateExternalSkill("dragon_palm");
        var book = CreateItem(
            "dragon_book",
            ItemType.SkillBook,
            [new GrantExternalSkillItemUseEffectDefinition(skill.Id, Level: 5)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 5)]);
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
    public void Use_InternalSkillBook_LearnsDefaultLevel10AndDoesNotConsume()
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
        Assert.Equal(10, hero.GetInternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void Use_InternalSkillBook_RespectsBookEffectLevelByDefault()
    {
        var skill = TestContentFactory.CreateInternalSkill("yijinjing");
        var book = CreateItem(
            "internal_book",
            ItemType.SkillBook,
            [new GrantInternalSkillItemUseEffectDefinition(skill.Id, Level: 5)]);
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
        Assert.Equal(5, hero.GetInternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void AnalyzeTarget_DisablesNewInternalSkillWhenInternalSkillCountAtLimit()
    {
        var knownSkill = TestContentFactory.CreateInternalSkill("known_internal");
        var newSkill = TestContentFactory.CreateInternalSkill("yijinjing");
        var book = CreateItem(
            "internal_book",
            ItemType.SkillBook,
            [new GrantInternalSkillItemUseEffectDefinition(newSkill.Id)]);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            internalSkills: [new InitialInternalSkillEntryDefinition(knownSkill)]);
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            internalSkills: [knownSkill, newSkill],
            items: [book]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig { MaxInternalSkillCount = 1 });
        var entry = state.Inventory.GetStack(book);

        var candidate = session.ItemUseService.AnalyzeTarget(entry, hero);

        Assert.False(candidate.CanUse);
        Assert.Equal("内功数量已达上限", candidate.Reason);
        Assert.Null(hero.GetInternalSkillLevel(newSkill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void Use_SpecialSkillBook_LearnsAndConsumesOne()
    {
        var skill = new SpecialSkillDefinition(
            "six_pulse",
            "six_pulse",
            "",
            SpecialSkillIntent.Support,
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
            [new GrantSpecialSkillItemUseEffectDefinition(skill.Id)],
            consumeOnUse: true);
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
        Assert.Empty(state.Inventory.Entries);
    }

    [Fact]
    public void Use_TalentBook_LearnsAndConsumesOne()
    {
        var talent = new TalentDefinition { Id = "iron_body", Name = "iron_body" };
        var book = CreateItem(
            "talent_book",
            ItemType.TalentBook,
            [new GrantTalentItemUseEffectDefinition(talent.Id)],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book, 2);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            growTemplates: [CreateDefaultGrowTemplate()],
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
    public void AnalyzeTarget_DisablesTalentBookWhenWuxueCapacityIsInsufficient()
    {
        var expensiveTalent = new TalentDefinition
        {
            Id = "expensive",
            Name = "expensive",
            Point = 30,
        };
        var book = CreateItem(
            "talent_book",
            ItemType.TalentBook,
            [new GrantTalentItemUseEffectDefinition(expensiveTalent.Id)],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            growTemplates:
            [
                TestContentFactory.CreateGrowTemplate(
                    CharacterExperienceProgression.DefaultGrowTemplateId,
                    new Dictionary<StatType, int>
                    {
                        [StatType.Wuxue] = 0,
                    }),
            ],
            talents: [expensiveTalent],
            items: [book]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(book);

        var candidate = session.ItemUseService.AnalyzeTarget(entry, hero);
        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.False(candidate.CanUse);
        Assert.Equal("武学常识不足，需要30", candidate.Reason);
        Assert.False(result.Success);
        Assert.Equal("武学常识不足，需要30", result.Message);
        Assert.False(hero.HasTalent(expensiveTalent.Id));
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
            ],
            consumeOnUse: true);
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
        IReadOnlyList<ItemUseEffectDefinition> effects,
        bool consumeOnUse = false) =>
        new()
        {
            Id = id,
            Name = id,
            Type = type,
            ConsumeOnUse = consumeOnUse,
            UseEffects = effects,
        };

    private static GrowTemplateDefinition CreateDefaultGrowTemplate() =>
        TestContentFactory.CreateGrowTemplate(
            CharacterExperienceProgression.DefaultGrowTemplateId,
            new Dictionary<StatType, int>
            {
                [StatType.Wuxue] = 8,
            });
}
