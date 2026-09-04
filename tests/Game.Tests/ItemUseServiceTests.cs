using Game.Application;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Story;

namespace Game.Tests;

public sealed class ItemUseServiceTests
{
    [Fact]
    public async Task Use_ExternalSkillBook_LearnsDefaultLevel10AndDoesNotConsume()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(10, hero.GetExternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public async Task Use_ExternalSkillBook_UpgradesKnownSkillBelowMaxAndDoesNotConsume()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(10, hero.GetExternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public async Task Use_ExternalSkillBook_UpgradesKnownSkillWhenExternalSkillCountAtLimit()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(10, hero.GetExternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public async Task Use_ExternalSkillBook_RespectsBookEffectLevelByDefault()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(5, hero.GetExternalSkillLevel(skill.Id));
    }

    [Fact]
    public async Task Use_ExternalSkillBook_IgnoresBookEffectLevelWhenConfigured()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(10, hero.GetExternalSkillLevel(skill.Id));
    }

    [Fact]
    public async Task Use_ExternalSkillBook_ClampsBookEffectLevelToCurrentMaxLevel()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(10, hero.GetExternalSkillLevel(skill.Id));
    }

    [Fact]
    public async Task AnalyzeTarget_DisablesNewExternalSkillWhenExternalSkillCountAtLimit()
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
        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

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
        Assert.Equal("该物品已无法带来新的效果", candidate.Reason);
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
        Assert.Equal("该物品已无法带来新的效果", candidate.Reason);
    }

    [Fact]
    public async Task Use_InternalSkillBook_LearnsDefaultLevel10AndDoesNotConsume()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(10, hero.GetInternalSkillLevel(skill.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public async Task Use_InternalSkillBook_RespectsBookEffectLevelByDefault()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

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
    public async Task Use_SpecialSkillBook_LearnsAndConsumesOne()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Contains(hero.GetSpecialSkills(), learned => learned.Definition.Id == skill.Id);
        Assert.Empty(state.Inventory.Entries);
    }

    [Fact]
    public async Task Use_TalentBook_LearnsAndConsumesOne()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.True(hero.HasTalent(talent.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public async Task AnalyzeTarget_DisablesTalentBookWhenWuxueCapacityIsInsufficient()
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
        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.False(candidate.CanUse);
        Assert.Equal("武学常识不足，需要30", candidate.Reason);
        Assert.False(result.Success);
        Assert.Equal("武学常识不足，需要30", result.Message);
        Assert.False(hero.HasTalent(expensiveTalent.Id));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public async Task Use_Booster_IncreasesMaxStatsAndConsumesOne()
    {
        var booster = CreateItem(
            "peach",
            ItemType.Booster,
            [
                new AddStatsItemUseEffectDefinition(new Dictionary<StatType, int>
                {
                    [StatType.MaxHp] = 100,
                    [StatType.MaxMp] = 50,
                    [StatType.Bili] = 5,
                }),
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(300, hero.GetBaseStat(StatType.MaxHp));
        Assert.Equal(130, hero.GetBaseStat(StatType.MaxMp));
        Assert.Equal(5, hero.GetBaseStat(StatType.Bili));
        Assert.Empty(state.Inventory.Entries);
    }

    [Fact]
    public async Task Use_Booster_RejectsCombinedStatChangesBeforeMutatingCharacter()
    {
        var booster = CreateItem(
            "poison",
            ItemType.Booster,
            [
                new AddStatsItemUseEffectDefinition(new Dictionary<StatType, int> { [StatType.Bili] = -60 }),
                new AddStatsItemUseEffectDefinition(new Dictionary<StatType, int> { [StatType.Bili] = -60 }),
            ],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int> { [StatType.Bili] = 100 });
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(booster);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            items: [booster]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(booster);

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.False(result.Success);
        Assert.Equal("臂力不能低于0", result.Message);
        Assert.Equal(100, hero.GetBaseStat(StatType.Bili));
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public async Task Use_MultiEffectBook_RequiresConfirmationAndAppliesOnlyNewEffects()
    {
        var externalSkill = TestContentFactory.CreateExternalSkill("known_external");
        var internalSkill = TestContentFactory.CreateInternalSkill("known_internal");
        var specialSkill = new SpecialSkillDefinition(
            "known_special",
            "known_special",
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
        var knownTalent = new TalentDefinition { Id = "known_talent", Name = "known_talent", Point = 27 };
        var newTalent = new TalentDefinition { Id = "new_talent", Name = "new_talent", Point = 1 };
        var book = CreateItem(
            "mixed_book",
            ItemType.TalentBook,
            [
                new GrantExternalSkillItemUseEffectDefinition(externalSkill.Id, Level: 5),
                new GrantInternalSkillItemUseEffectDefinition(internalSkill.Id, Level: 5),
                new GrantSpecialSkillItemUseEffectDefinition(specialSkill.Id),
                new GrantTalentItemUseEffectDefinition(knownTalent.Id),
                new GrantTalentItemUseEffectDefinition(newTalent.Id),
            ],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(externalSkill, Level: 10)],
            internalSkills: [new InitialInternalSkillEntryDefinition(internalSkill, Level: 10)]);
        var state = CreateStateWithHero(heroDefinition, out var hero);
        hero.LearnSpecialSkill(specialSkill);
        hero.LearnTalent(knownTalent);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [externalSkill],
            internalSkills: [internalSkill],
            specialSkills: [specialSkill],
            growTemplates: [CreateDefaultGrowTemplate()],
            talents: [knownTalent, newTalent],
            items: [book]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                MaxExternalSkillCount = 1,
                MaxInternalSkillCount = 1,
            });
        var entry = state.Inventory.GetStack(book);

        var candidate = session.ItemUseService.AnalyzeTarget(entry, hero);
        var unconfirmedResult = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(candidate.CanUse);
        Assert.True(candidate.RequiresConfirmation);
        Assert.Equal(4, candidate.SkippedEffects.Count);
        Assert.False(unconfirmedResult.Success);
        Assert.Equal("该物品有部分效果无法生效，请确认后使用。", unconfirmedResult.Message);
        Assert.False(hero.HasTalent(newTalent.Id));
        Assert.Equal(1, entry.Quantity);

        var confirmedResult = await session.ItemUseService.UseAsync(entry, hero.Id, true);

        Assert.True(confirmedResult.Success);
        Assert.True(hero.HasTalent(newTalent.Id));
        Assert.Equal(10, hero.GetExternalSkillLevel(externalSkill.Id));
        Assert.Equal(10, hero.GetInternalSkillLevel(internalSkill.Id));
        Assert.Empty(state.Inventory.Entries);
    }

    [Fact]
    public async Task Use_BookWithOnlyRedundantEffects_IsDisabledAndNotConsumed()
    {
        var talent = new TalentDefinition { Id = "known_talent", Name = "known_talent" };
        var book = CreateItem(
            "known_talent_book",
            ItemType.TalentBook,
            [new GrantTalentItemUseEffectDefinition(talent.Id)],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        hero.LearnTalent(talent);
        state.Inventory.AddItem(book);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            growTemplates: [CreateDefaultGrowTemplate()],
            talents: [talent],
            items: [book]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(book);

        var candidate = session.ItemUseService.AnalyzeTarget(entry, hero);
        var result = await session.ItemUseService.UseAsync(entry, hero.Id, true);

        Assert.False(candidate.CanUse);
        Assert.False(candidate.RequiresConfirmation);
        Assert.Equal("该物品已无法带来新的效果", candidate.Reason);
        Assert.False(result.Success);
        Assert.Equal("该物品已无法带来新的效果", result.Message);
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public async Task Use_Booster_SucceedsWhenStoryGrantedSkillsExceedBookLimits()
    {
        var externalSkills = new[]
        {
            TestContentFactory.CreateExternalSkill("external_1"),
            TestContentFactory.CreateExternalSkill("external_2"),
        };
        var internalSkills = new[]
        {
            TestContentFactory.CreateInternalSkill("internal_1"),
            TestContentFactory.CreateInternalSkill("internal_2"),
        };
        var booster = CreateItem(
            "peach",
            ItemType.Booster,
            [new AddStatsItemUseEffectDefinition(new Dictionary<StatType, int> { [StatType.MaxHp] = 100 })],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.MaxHp] = 200,
            });
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(booster);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: externalSkills,
            internalSkills: internalSkills,
            items: [booster]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                MaxExternalSkillCount = 1,
                MaxInternalSkillCount = 1,
            });
        foreach (var skill in externalSkills)
        {
            session.CharacterService.GrantExternalSkill(hero, skill.Id);
        }
        foreach (var skill in internalSkills)
        {
            session.CharacterService.GrantInternalSkill(hero, skill.Id);
        }
        var entry = state.Inventory.GetStack(booster);

        var candidate = session.ItemUseService.AnalyzeTarget(entry, hero);
        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(candidate.CanUse);
        Assert.True(result.Success);
        Assert.Equal(300, hero.GetBaseStat(StatType.MaxHp));
        Assert.Empty(state.Inventory.Entries);
    }

    [Fact]
    public async Task Use_Equipment_ReplacesOccupiedSlotAndReturnsOldEquipmentToInventory()
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

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(newSword.Id, hero.GetEquipment(EquipmentSlotType.Weapon)?.Definition.Id);
        var returned = Assert.IsType<StackInventoryEntry>(Assert.Single(state.Inventory.Entries));
        Assert.Equal(oldSword.Id, returned.Definition.Id);
        Assert.Equal(1, returned.Quantity);
    }

    [Fact]
    public async Task UseAsync_RunStory_PassesScopedTargetThroughCallAndConsumesBeforeExecution()
    {
        var item = CreateItem(
            "story_booster",
            ItemType.Booster,
            [new RunStoryItemUseEffectDefinition("item_story")],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int> { [StatType.MaxHp] = 200 });
        var allyDefinition = TestContentFactory.CreateCharacterDefinition(
            "ally",
            new Dictionary<StatType, int> { [StatType.MaxHp] = 200 });
        var state = CreateStateWithHero(heroDefinition, out var hero);
        var ally = TestContentFactory.CreateCharacterInstance(
            "ally",
            allyDefinition,
            state.EquipmentInstanceFactory);
        state.Party.AddMember(ally);
        state.Inventory.AddItem(item);
        var story = new StoryScript(
            StoryScript.CurrentVersion,
            [
                new Segment("item_story", [new CallStep("apply_item_effect")]),
                new Segment(
                    "apply_item_effect",
                    [
                        new CommandStep(new ExpressionParser().ParseCall("change_stat(" + ItemUseService.ItemTargetCharacterIdVariable + ", 'maxhp', 100)")),
                    ]),
                new Segment(
                    "probe_context",
                    [
                        new CommandStep(new ExpressionParser().ParseCall("change_stat(" + ItemUseService.ItemTargetCharacterIdVariable + ", 'maxhp', 1)")),
                    ]),
            ]);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition, allyDefinition],
            storyScripts: [story],
            items: [item]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(item);

        var result = await session.ItemUseService.UseAsync(entry, ally.Id);

        Assert.True(result.Success);
        Assert.Equal(200, hero.GetBaseStat(StatType.MaxHp));
        Assert.Equal(300, ally.GetBaseStat(StatType.MaxHp));
        Assert.Empty(state.Inventory.Entries);
        Assert.False(state.Story.TryGetVariable(
            ItemUseService.ItemTargetCharacterIdVariable,
            out _));
        await Assert.ThrowsAsync<ExpressionEvaluationException>(
            () => session.StoryService.ExecuteAsync("probe_context"));
    }

    [Fact]
    public async Task UseAsync_RunStory_KeepsConsumedItemWhenStoryFails()
    {
        var item = CreateItem(
            "broken_story_item",
            ItemType.Utility,
            [new RunStoryItemUseEffectDefinition("broken_story")],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(item);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [new Segment("broken_story", [new CommandStep(new ExpressionParser().ParseCall("unsupported()"))])]),
            ],
            items: [item]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(item);

        await Assert.ThrowsAsync<ExpressionBindingException>(
            () => session.ItemUseService.UseAsync(entry, hero.Id));

        Assert.Empty(state.Inventory.Entries);
    }

    [Fact]
    public async Task UseAsync_RunStory_KeepItemSkipsConsumption()
    {
        var item = CreateItem(
            "reusable_phone",
            ItemType.Utility,
            [new RunStoryItemUseEffectDefinition("keep_story", KeepItem: true)],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(item);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            storyScripts:
            [
                new StoryScript(
                    StoryScript.CurrentVersion,
                    [new Segment("keep_story", [])]),
            ],
            items: [item]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(item);

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Single(state.Inventory.Entries);
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

    [Fact]
    public async Task Use_TitleItem_GrantsTitleAndConsumes()
    {
        var title = new CharacterTitleDefinition { Id = "襄阳义士", Name = "襄阳义士" };
        var item = CreateItem(
            "襄阳义士称号",
            ItemType.SpecialSkillBook,
            [new GrantTitleItemUseEffectDefinition(title.Id)],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(item);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            characterTitles: [title],
            items: [item]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(item);

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Contains(hero.Titles, owned => owned.Id == title.Id);
        Assert.Empty(state.Inventory.Entries);
    }

    [Fact]
    public async Task Use_TitleItem_AlreadyOwned_FailsWithoutConsuming()
    {
        var title = new CharacterTitleDefinition { Id = "襄阳义士", Name = "襄阳义士" };
        var item = CreateItem(
            "襄阳义士称号",
            ItemType.SpecialSkillBook,
            [new GrantTitleItemUseEffectDefinition(title.Id)],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        hero.AddTitle(title);
        state.Inventory.AddItem(item);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            characterTitles: [title],
            items: [item]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(item);

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.False(result.Success);
        Assert.Single(state.Inventory.Entries);
    }

    [Fact]
    public async Task Use_PortraitItem_SetsPortraitAndConsumes()
    {
        var resource = new ResourceDefinition { Id = "头像.雄霸", Group = "head" };
        var item = CreateItem(
            "雄霸面具",
            ItemType.QuestItem,
            [new SetPortraitItemUseEffectDefinition(resource.Id)],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        state.Inventory.AddItem(item);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            resources: [resource],
            items: [item]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(item);

        var result = await session.ItemUseService.UseAsync(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(resource.Id, hero.Portrait);
        Assert.Empty(state.Inventory.Entries);
    }

    [Fact]
    public async Task Use_RandomBox_GrantsExactlyOneEntryAndConsumes()
    {
        var rewards = new[]
        {
            CreateItem("猪肉", ItemType.Consumable, []),
            CreateItem("王母蟠桃", ItemType.Consumable, []),
            CreateItem("雕羽", ItemType.Consumable, []),
        };
        var box = CreateItem(
            "宝箱",
            ItemType.QuestItem,
            [new RandomItemItemUseEffectDefinition(
                rewards.Select(reward => new RandomItemEntry(reward.Id, 1)).ToList())],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out _);
        state.Inventory.AddItem(box);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            items: [box, .. rewards]);
        var session = new GameSession(state, repository, randomService: new Game.Core.Engine.DeterministicRandomService(7));
        var entry = state.Inventory.GetStack(box);

        var result = await session.ItemUseService.UseAsync(entry, "hero");

        Assert.True(result.Success);
        Assert.Single(state.Inventory.Entries);
        var grantedRewards = rewards.Where(reward => state.Inventory.ContainsStack(reward, 1)).ToList();
        Assert.Single(grantedRewards);
    }

    [Fact]
    public void Analyze_QuestItemWithRunStoryEffect_IsSupported()
    {
        var item = CreateItem(
            "水底宝箱",
            ItemType.QuestItem,
            [new RunStoryItemUseEffectDefinition("水底宝箱剧情")],
            consumeOnUse: true);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out _);
        state.Inventory.AddItem(item);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            items: [item]);
        var session = new GameSession(state, repository);
        var entry = state.Inventory.GetStack(item);

        var analysis = session.ItemUseService.Analyze(entry);

        Assert.True(analysis.IsSupported);
    }

    [Fact]
    public void ResolveAutoTarget_RoleKeyPinsPartyMember()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        var pinnedItem = CreateItem(
            "圣火光明袍",
            ItemType.QuestItem,
            [],
            consumeOnUse: false) with
        {
            Requirements = [new RoleKeyItemRequirementDefinition("hero")],
        };
        state.Inventory.AddItem(pinnedItem);
        var strangerPinnedItem = CreateItem(
            "龙痕剑",
            ItemType.QuestItem,
            [],
            consumeOnUse: false) with
        {
            Requirements = [new RoleKeyItemRequirementDefinition("雷震")],
        };
        state.Inventory.AddItem(strangerPinnedItem);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            items: [pinnedItem, strangerPinnedItem]);
        var session = new GameSession(state, repository);

        Assert.Equal(
            hero.Id,
            session.ItemUseService.ResolveAutoTargetCharacterId(state.Inventory.GetStack(pinnedItem)));
        Assert.Null(
            session.ItemUseService.ResolveAutoTargetCharacterId(state.Inventory.GetStack(strangerPinnedItem)));
    }

    [Fact]
    public void ResolveAutoTarget_InventoryEffectTargetsFirstPartyMember()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = CreateStateWithHero(heroDefinition, out var hero);
        var box = CreateItem(
            "宝箱",
            ItemType.QuestItem,
            [new RandomItemItemUseEffectDefinition([new RandomItemEntry("猪肉", 1)])],
            consumeOnUse: true);
        state.Inventory.AddItem(box);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            items: [box]);
        var session = new GameSession(state, repository);

        Assert.Equal(
            hero.Id,
            session.ItemUseService.ResolveAutoTargetCharacterId(state.Inventory.GetStack(box)));
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
