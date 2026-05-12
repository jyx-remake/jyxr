using Game.Application;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Persistence;
using Game.Core.Story;

namespace Game.Tests;

public sealed class SessionEventsTests
{
    [Fact]
    public void EnterMap_PublishesMapChangedEvent()
    {
        var repository = TestContentFactory.CreateRepository(
            maps:
            [
                new MapDefinition
                {
                    Id = "world",
                    Name = "world",
                    Kind = MapKind.Large,
                },
            ]);
        var session = new GameSession(new GameState(), repository);
        var publishedEvents = CollectPublishedEvents(session);

        session.MapService.EnterMap("world");

        var mapChanged = Assert.Single(publishedEvents.OfType<MapChangedEvent>());
        Assert.Equal("world", mapChanged.MapId);
    }

    [Fact]
    public void InteractWithLocation_MapTransition_PublishesClockAndMapChangedEvents()
    {
        var repository = TestContentFactory.CreateRepository(
            maps:
            [
                new MapDefinition
                {
                    Id = "world",
                    Name = "world",
                    Kind = MapKind.Large,
                    Locations =
                    [
                        new MapLocationDefinition
                        {
                            Id = "inn_gate",
                            Name = "inn_gate",
                            Position = new MapPosition(20, 20),
                            Events =
                            [
                                new MapEventDefinition
                                {
                                    Type = "map",
                                    TargetId = "inn",
                                    Probability = 100,
                                },
                            ],
                        },
                    ],
                },
                new MapDefinition
                {
                    Id = "inn",
                    Name = "inn",
                    Kind = MapKind.Small,
                },
            ]);
        var state = new GameState();
        state.Location.SetLargeMapPosition("world", MapPosition.Zero);
        var session = new GameSession(state, repository);
        var publishedEvents = CollectPublishedEvents(session);

        var location = session.MapService.EnterMap("world").Locations.Single();
        publishedEvents.Clear();

        session.MapService.InteractWithLocation(location);

        Assert.Collection(
            publishedEvents,
            sessionEvent => Assert.IsType<ClockChangedEvent>(sessionEvent),
            sessionEvent =>
            {
                var mapChanged = Assert.IsType<MapChangedEvent>(sessionEvent);
                Assert.Equal("inn", mapChanged.MapId);
            });
    }

    [Fact]
    public async Task StoryCommandDispatcher_PublishesDomainEventsForChangedState()
    {
        var attack = TestContentFactory.CreateExternalSkill("attack");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero", externalSkills: []);
        var allyDefinition = TestContentFactory.CreateCharacterDefinition("ally", externalSkills: [new InitialExternalSkillEntryDefinition(attack)]);
        var potion = new NormalItemDefinition
        {
            Id = "potion",
            Name = "potion",
            Type = ItemType.Consumable,
        };
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition, allyDefinition],
            externalSkills: [attack],
            items: [potion],
            resources:
            [
                new ResourceDefinition
                {
                    Id = "nick.初出茅庐",
                    Group = "nick",
                    Value = "初入江湖。",
                },
            ]);
        var state = new GameState();
        var party = new Party();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        party.AddMember(hero);
        state.SetParty(party);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, new RecordingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        await dispatcher.ExecuteCommandAsync("item", [ExprValue.FromString("potion")], default);
        await dispatcher.ExecuteCommandAsync("get_money", [ExprValue.FromNumber(20)], default);
        await dispatcher.ExecuteCommandAsync("cost_day", [ExprValue.FromNumber(2)], default);
        await dispatcher.ExecuteCommandAsync("log", [ExprValue.FromString("初出茅庐")], default);
        await dispatcher.ExecuteCommandAsync("daode", [ExprValue.FromNumber(5)], default);
        await dispatcher.ExecuteCommandAsync("haogan", [ExprValue.FromNumber(12)], default);
        await dispatcher.ExecuteCommandAsync("get_point", [ExprValue.FromString("hero"), ExprValue.FromNumber(3)], default);
        await dispatcher.ExecuteCommandAsync("upgrade", [ExprValue.FromString("bili"), ExprValue.FromString("hero"), ExprValue.FromNumber(2)], default);
        await dispatcher.ExecuteCommandAsync("nick", [ExprValue.FromString("初出茅庐")], default);
        await dispatcher.ExecuteCommandAsync("join", [ExprValue.FromString("ally")], default);

        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is InventoryChangedEvent);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is CurrencyChangedEvent);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is ClockChangedEvent);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is JournalChangedEvent);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is AdventureStateChangedEvent);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is CharacterChangedEvent);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is PartyChangedEvent);
        Assert.Contains(
            publishedEvents,
            static sessionEvent => sessionEvent is AchievementUnlockedEvent { AchievementId: "初出茅庐" });
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is ProfileChangedEvent);
        Assert.Equal(2, hero.BaseStats[StatType.Bili]);
        Assert.Equal(3, hero.UnspentStatPoints);
        Assert.True(session.Profile.IsAchievementUnlocked("初出茅庐"));
        Assert.Equal(55, session.State.Adventure.Morality);
        Assert.Equal(62, session.State.Adventure.Favorability);
    }

    [Fact]
    public async Task StoryCommandDispatcher_GrowTemplate_UpdatesCharacterInstanceState()
    {
        var defaultGrowth = TestContentFactory.CreateGrowTemplate("default", new Dictionary<StatType, int>());
        var wandererGrowth = TestContentFactory.CreateGrowTemplate("wanderer", new Dictionary<StatType, int>());
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero", growTemplate: "default");
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            growTemplates: [defaultGrowth, wandererGrowth]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, new RecordingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        await dispatcher.ExecuteCommandAsync(
            "growtemplate",
            [ExprValue.FromString("hero"), ExprValue.FromString("wanderer")],
            default);

        Assert.Equal("wanderer", hero.GrowTemplateId);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is CharacterChangedEvent { CharacterId: "hero" });
    }

    [Fact]
    public async Task StoryCommandDispatcher_SetTimeKeyRegistersTimer()
    {
        var repository = TestContentFactory.CreateRepository(
            storyScripts:
            [
                new StoryScript(1, [new Segment("quiz_timeout", [])]),
            ]);
        var session = new GameSession(new GameState(), repository);
        var dispatcher = new StoryCommandDispatcher(session, new RecordingRuntimeHost());

        await dispatcher.ExecuteCommandAsync(
            "set_time_key",
            [
                ExprValue.FromString("quiz_done"),
                ExprValue.FromNumber(2),
                ExprValue.FromString("quiz_timeout"),
            ],
            default);

        var timeKey = Assert.Single(session.State.Story.TimeKeys.Values);
        Assert.Equal("quiz_done", timeKey.Key);
        Assert.Equal(2, timeKey.LimitDays);
        Assert.Equal("quiz_timeout", timeKey.TargetStoryId);
    }

    [Fact]
    public async Task StoryCommandDispatcher_ChangeFemaleName_CreatesReserveCharacterWhenInactive()
    {
        var femaleDefinition = TestContentFactory.CreateCharacterDefinition("女主");
        var repository = TestContentFactory.CreateRepository(characters: [femaleDefinition]);
        var session = new GameSession(new GameState(), repository);
        var dispatcher = new StoryCommandDispatcher(session, new RecordingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        await dispatcher.ExecuteCommandAsync("change_female_name", [ExprValue.FromString("玲兰")], default);

        var female = Assert.Single(session.State.Party.Reserves);
        Assert.Equal("女主", female.Id);
        Assert.Equal("玲兰", female.Name);
        Assert.Empty(session.State.Party.Members);
        Assert.Empty(session.State.Party.Followers);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is PartyChangedEvent);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is CharacterChangedEvent { CharacterId: "女主" });

        await dispatcher.ExecuteCommandAsync("change_female_name", [ExprValue.FromString("阿兰")], default);

        Assert.Same(female, Assert.Single(session.State.Party.Reserves));
        Assert.Equal("阿兰", female.Name);
    }

    [Fact]
    public async Task StoryCommandDispatcher_MaxLevel_PublishesSingleToastWithoutChangingCharacters()
    {
        var skill = TestContentFactory.CreateExternalSkill("starter_sword");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [skill]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, new RecordingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        await dispatcher.ExecuteCommandAsync("maxlevel", [ExprValue.FromString("starter_sword"), ExprValue.FromNumber(2)], default);

        var toast = Assert.Single(publishedEvents.OfType<ToastRequestedEvent>());
        Assert.Equal("武学精通【starter_sword】+ 2", toast.Message);
        Assert.DoesNotContain(publishedEvents, static sessionEvent => sessionEvent is CharacterChangedEvent);
        Assert.Null(hero.GetExternalSkillLevel("starter_sword"));
    }

    [Fact]
    public async Task StoryCommandDispatcher_Remove_RemovesSkillsAndTalents()
    {
        var externalSkill = TestContentFactory.CreateExternalSkill(
            "starter_sword",
            affixes:
            [
                new SkillAffixDefinition(new StatModifierAffix(StatType.Gengu, ModifierValue.Add(2))),
            ]);
        var internalSkill = TestContentFactory.CreateInternalSkill(
            "guarded",
            affixes:
            [
                new SkillAffixDefinition(
                    new StatModifierAffix(StatType.Gengu, ModifierValue.Add(6)),
                    RequiresEquippedInternalSkill: true),
            ]);
        var talent = new TalentDefinition
        {
            Id = "iron_body",
            Name = "iron_body",
            Affixes = [new StatModifierAffix(StatType.Gengu, ModifierValue.Add(3))],
        };
        var specialSkill = new SpecialSkillDefinition(
            "flash_step",
            "flash_step",
            "",
            "",
            0,
            new SkillCostDefinition(),
            new SkillTargetingDefinition(),
            "",
            "",
            null,
            []);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.Gengu] = 10,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(externalSkill)],
            internalSkills: [new InitialInternalSkillEntryDefinition(internalSkill, Equipped: true)],
            talents: [talent],
            specialSkills: [specialSkill]);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [externalSkill],
            internalSkills: [internalSkill],
            talents: [talent],
            specialSkills: [specialSkill]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, new RecordingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        Assert.Equal(21, hero.GetStat(StatType.Gengu));

        await dispatcher.ExecuteCommandAsync(
            "remove",
            [ExprValue.FromString("skill"), ExprValue.FromString("hero"), ExprValue.FromString("starter_sword")],
            default);
        await dispatcher.ExecuteCommandAsync(
            "remove",
            [ExprValue.FromString("internal"), ExprValue.FromString("hero"), ExprValue.FromString("guarded")],
            default);
        await dispatcher.ExecuteCommandAsync(
            "remove",
            [ExprValue.FromString("talent"), ExprValue.FromString("hero"), ExprValue.FromString("iron_body")],
            default);
        await dispatcher.ExecuteCommandAsync(
            "remove",
            [ExprValue.FromString("skill"), ExprValue.FromString("hero"), ExprValue.FromString("flash_step")],
            default);

        Assert.Null(hero.GetExternalSkillLevel("starter_sword"));
        Assert.Null(hero.GetInternalSkillLevel("guarded"));
        Assert.Null(hero.EquippedInternalSkillId);
        Assert.False(hero.HasTalent("iron_body"));
        Assert.False(hero.HasEffectiveTalent("iron_body"));
        Assert.Empty(hero.SpecialSkills);
        Assert.Equal(10, hero.GetStat(StatType.Gengu));
        Assert.Equal(
            ["hero", "hero", "hero", "hero"],
            publishedEvents.OfType<CharacterChangedEvent>().Select(evt => evt.CharacterId).ToArray());
    }

    [Fact]
    public async Task StoryCommandDispatcher_IgnoresTouchAndRankCommands()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var dispatcher = new StoryCommandDispatcher(session, new ThrowingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        await dispatcher.ExecuteCommandAsync("touch", [ExprValue.FromString("cg.touch")], default);
        await dispatcher.ExecuteCommandAsync("rank", [ExprValue.FromNumber(999)], default);

        Assert.Empty(publishedEvents);
        Assert.Equal(0d, session.State.Adventure.Rank);
    }

    [Fact]
    public async Task StoryCommandDispatcher_xilian_NoEligibleEquipment_JumpsNoEquipment_NoCharge_NoChoice()
    {
        var state = new GameState();
        state.Currency.AddGold(1);
        var session = new GameSession(state, TestContentFactory.CreateRepository());
        var dispatcher = new StoryCommandDispatcher(session, new ThrowingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        var result = await dispatcher.ExecuteCommandAsync("xilian", [ExprValue.FromNumber(0)], default);

        Assert.Equal("洗练_没有装备", result.JumpTarget);
        Assert.Equal(1, state.Currency.Gold);
        Assert.Empty(publishedEvents);
    }

    [Fact]
    public async Task xilian_CancelReplacement_ChargesGold_ReturnsXilianChoice_DoesNotChangeInventory()
    {
        var oldAffix = new StatModifierAffix(StatType.Attack, ModifierValue.Add(10));
        var equipment = TestContentFactory.CreateEquipment("test_sword");
        var repository = TestContentFactory.CreateRepository(
            equipment: [equipment],
            equipmentRandomAffixTables: [CreateSpeedAffixTable("0.125")]);
        var state = new GameState();
        state.Currency.AddGold(1);
        var entry = Assert.IsType<EquipmentInstanceInventoryEntry>(
            state.Inventory.AddEquipmentInstance(state.EquipmentInstanceFactory.Create(equipment, [oldAffix])));
        var host = new RecordingRuntimeHost(0, 0, 8);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, host);
        var publishedEvents = CollectPublishedEvents(session);

        var result = await dispatcher.ExecuteCommandAsync("xilian", [ExprValue.FromNumber(0)], default);

        Assert.Equal("洗练选择", result.JumpTarget);
        Assert.Equal(0, state.Currency.Gold);
        Assert.Equal([oldAffix], entry.Equipment.ExtraAffixes);
        Assert.Single(publishedEvents.OfType<CurrencyChangedEvent>());
        Assert.Empty(publishedEvents.OfType<InventoryChangedEvent>());
    }

    [Fact]
    public async Task xilian_ReplacesSelectedAffixGroup_PreservesEquipmentInstanceAndEntryOrder()
    {
        var oldAffix = new StatModifierAffix(StatType.Attack, ModifierValue.Add(10));
        var equipment = TestContentFactory.CreateEquipment("test_sword");
        var repository = TestContentFactory.CreateRepository(
            equipment: [equipment],
            equipmentRandomAffixTables: [CreateSpeedAffixTable("0.125")]);
        var state = new GameState();
        state.Currency.AddGold(1);
        var entry = Assert.IsType<EquipmentInstanceInventoryEntry>(
            state.Inventory.AddEquipmentInstance(state.EquipmentInstanceFactory.Create(equipment, [oldAffix])));
        var originalEquipment = entry.Equipment;
        var originalEquipmentId = entry.Equipment.Id;
        var originalEntryNumber = entry.EntryNumber;
        var host = new RecordingRuntimeHost(0, 0, 0);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, host);
        var publishedEvents = CollectPublishedEvents(session);

        var result = await dispatcher.ExecuteCommandAsync("xilian", [ExprValue.FromNumber(0)], default);

        Assert.Equal("洗练_洗练成功", result.JumpTarget);
        var refinedEntry = Assert.IsType<EquipmentInstanceInventoryEntry>(Assert.Single(state.Inventory.Entries));
        Assert.Equal(originalEntryNumber, refinedEntry.EntryNumber);
        Assert.Equal(originalEquipmentId, refinedEntry.Equipment.Id);
        Assert.Same(originalEquipment, refinedEntry.Equipment);
        var speedAffix = Assert.IsType<StatModifierAffix>(Assert.Single(refinedEntry.Equipment.ExtraAffixes));
        Assert.Equal(StatType.Speed, speedAffix.Stat);
        Assert.Equal(0, state.Currency.Gold);
        Assert.Single(publishedEvents.OfType<CurrencyChangedEvent>());
        Assert.Single(publishedEvents.OfType<InventoryChangedEvent>());
        Assert.Contains(host.Commands, static command => command.Name == "effect" && command.Args[0].AsString("effect") == "音效.装备");
    }

    [Fact]
    public async Task xilian_GeneratesEightCandidatesPlusCancel_AllowsDuplicateCandidateTexts()
    {
        var oldAffix = new StatModifierAffix(StatType.Attack, ModifierValue.Add(10));
        var equipment = TestContentFactory.CreateEquipment("test_sword");
        var repository = TestContentFactory.CreateRepository(
            equipment: [equipment],
            equipmentRandomAffixTables: [CreateSpeedAffixTable("0.125")]);
        var state = new GameState();
        state.Currency.AddGold(1);
        state.Inventory.AddEquipmentInstance(state.EquipmentInstanceFactory.Create(equipment, [oldAffix]));
        var host = new RecordingRuntimeHost(0, 0, 8);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, host);

        await dispatcher.ExecuteCommandAsync("xilian", [ExprValue.FromNumber(0)], default);

        var candidateChoice = host.Choices[2];
        Assert.Equal(9, candidateChoice.Options.Count);
        Assert.Equal(
            Enumerable.Repeat("集气速度 +0.125", 8).Append("不替换了").ToArray(),
            candidateChoice.Options.Select(static option => option.Text).ToArray());
    }

    [Fact]
    public async Task xilian_AvoidsCurrentEquipmentAffixTexts_WhenGeneratingCandidates()
    {
        var oldAffix = new StatModifierAffix(StatType.Speed, ModifierValue.Add(0.125));
        var equipment = TestContentFactory.CreateEquipment("test_sword");
        var repository = TestContentFactory.CreateRepository(
            equipment: [equipment],
            equipmentRandomAffixTables: [CreateSpeedAffixTable("0.125")]);
        var state = new GameState();
        state.Currency.AddGold(1);
        state.Inventory.AddEquipmentInstance(state.EquipmentInstanceFactory.Create(equipment, [oldAffix]));
        var host = new RecordingRuntimeHost(0, 0);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, host);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.ExecuteCommandAsync("xilian", [ExprValue.FromNumber(0)], default));

        Assert.Equal(1, state.Currency.Gold);
        Assert.Equal(2, host.Choices.Count);
    }

    [Fact]
    public async Task xilian_ReplacesMergedPairAsOneLogicalAffix()
    {
        var equipment = TestContentFactory.CreateEquipment("test_sword");
        var repository = TestContentFactory.CreateRepository(
            equipment: [equipment],
            equipmentRandomAffixTables:
            [
                new EquipmentRandomAffixTableDefinition
                {
                    MinItemLevel = 1,
                    MaxItemLevel = 1,
                    Options =
                    [
                        new EquipmentRandomAffixOptionDefinition
                        {
                            Kind = EquipmentRandomAffixKind.DefenceCombo,
                            Weight = 1,
                            Ranges =
                            [
                                new EquipmentRandomAffixRangeDefinition(8, 8),
                                new EquipmentRandomAffixRangeDefinition(3, 3),
                            ],
                        },
                    ],
                },
            ]);
        var state = new GameState();
        state.Currency.AddGold(1);
        state.Inventory.AddEquipmentInstance(state.EquipmentInstanceFactory.Create(
            equipment,
            [
                new StatModifierAffix(StatType.Attack, ModifierValue.Add(10)),
                new StatModifierAffix(StatType.CritChance, ModifierValue.Add(0.02)),
            ]));
        var host = new RecordingRuntimeHost(0, 0, 0);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, host);

        var result = await dispatcher.ExecuteCommandAsync("xilian", [ExprValue.FromNumber(0)], default);

        Assert.Equal("洗练_洗练成功", result.JumpTarget);
        var refinedEntry = Assert.IsType<EquipmentInstanceInventoryEntry>(Assert.Single(state.Inventory.Entries));
        Assert.Collection(
            refinedEntry.Equipment.ExtraAffixes,
            affix =>
            {
                var statAffix = Assert.IsType<StatModifierAffix>(affix);
                Assert.Equal(StatType.Defence, statAffix.Stat);
            },
            affix =>
            {
                var statAffix = Assert.IsType<StatModifierAffix>(affix);
                Assert.Equal(StatType.AntiCritChance, statAffix.Stat);
            });
        Assert.Equal(["攻击力 +10，暴击率 +2%"], host.Choices[1].Options.Select(static option => option.Text).ToArray());
    }

    [Fact]
    public async Task StoryCommandDispatcher_TodoCommands_PublishToast()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var dispatcher = new StoryCommandDispatcher(session, new ThrowingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("whac_a_mole")], default);
        await dispatcher.ExecuteCommandAsync("newbie", [], default);
        await dispatcher.ExecuteCommandAsync("tower", [ExprValue.FromString("tower")], default);
        await dispatcher.ExecuteCommandAsync("huashan", [ExprValue.FromString("huashan")], default);
        await dispatcher.ExecuteCommandAsync("trial", [ExprValue.FromString("trial")], default);
        await dispatcher.ExecuteCommandAsync("zhenlongqiju", [], default);
        await dispatcher.ExecuteCommandAsync("arena", [ExprValue.FromString("arena_单挑战")], default);

        Assert.Equal(
            [
                "game指令暂未实现",
                "newbie指令暂未实现",
                "tower指令暂未实现",
                "huashan指令暂未实现",
                "trial指令暂未实现",
                "zhenlongqiju指令暂未实现",
                "arena指令暂未实现",
            ],
            publishedEvents.OfType<ToastRequestedEvent>().Select(evt => evt.Message).ToArray());
    }

    [Fact]
    public async Task StoryCommandDispatcher_NextZhoumu_ForwardsToHost()
    {
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository());
        var host = new RecordingRuntimeHost();
        var dispatcher = new StoryCommandDispatcher(session, host);

        await dispatcher.ExecuteCommandAsync("nextzhoumu", [], default);

        var command = Assert.Single(host.Commands);
        Assert.Equal("nextzhoumu", command.Name);
        Assert.Empty(command.Args);
    }

    [Fact]
    public async Task StoryCommandDispatcher_MinusMaxPoints_UsesLegacyScalingSemantics()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 10,
                [StatType.Jianfa] = 11,
                [StatType.Daofa] = 12,
                [StatType.Qimen] = 13,
                [StatType.Bili] = 14,
                [StatType.Shenfa] = 15,
                [StatType.Wuxing] = 16,
                [StatType.Fuyuan] = 17,
                [StatType.Gengu] = 18,
                [StatType.Dingli] = 19,
                [StatType.MaxHp] = 40,
                [StatType.MaxMp] = 50,
            });
        var repository = TestContentFactory.CreateRepository(characters: [heroDefinition]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        session.CharacterService.GrantStatPoints("hero", 7);
        var dispatcher = new StoryCommandDispatcher(session, new ThrowingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        await dispatcher.ExecuteCommandAsync(
            "minus_maxpoints",
            [ExprValue.FromString("hero"), ExprValue.FromNumber(5)],
            default);

        Assert.Equal(5, hero.GetBaseStat(StatType.Quanzhang));
        Assert.Equal(5, hero.GetBaseStat(StatType.Jianfa));
        Assert.Equal(6, hero.GetBaseStat(StatType.Daofa));
        Assert.Equal(6, hero.GetBaseStat(StatType.Qimen));
        Assert.Equal(7, hero.GetBaseStat(StatType.Bili));
        Assert.Equal(7, hero.GetBaseStat(StatType.Shenfa));
        Assert.Equal(8, hero.GetBaseStat(StatType.Wuxing));
        Assert.Equal(8, hero.GetBaseStat(StatType.Fuyuan));
        Assert.Equal(9, hero.GetBaseStat(StatType.Gengu));
        Assert.Equal(9, hero.GetBaseStat(StatType.Dingli));
        Assert.Equal(20, hero.GetBaseStat(StatType.MaxHp));
        Assert.Equal(25, hero.GetBaseStat(StatType.MaxMp));
        Assert.Equal(3, hero.UnspentStatPoints);
        Assert.Contains(publishedEvents, static evt => evt is CharacterChangedEvent { CharacterId: "hero" });
    }

    [Fact]
    public async Task StoryCommandDispatcher_GetExp_UsesCharacterLevelingFlow()
    {
        var defaultGrowth = TestContentFactory.CreateGrowTemplate(
            "default",
            new Dictionary<StatType, int>
            {
                [StatType.Bili] = 1,
            });
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.Bili] = 10,
            });
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            growTemplates: [defaultGrowth]);
        var state = new GameState();
        var party = new Party();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        party.AddMember(hero);
        state.SetParty(party);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, new RecordingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        await dispatcher.ExecuteCommandAsync("get_exp", [ExprValue.FromString("hero"), ExprValue.FromNumber(CharacterLevelProgression.GetLevelUpExperience(1))], default);

        var leveledUp = Assert.Single(publishedEvents.OfType<CharacterLeveledUpEvent>());
        Assert.Equal("hero", leveledUp.CharacterId);
        Assert.Equal(2, hero.Level);
        Assert.Equal(2, leveledUp.NewLevel);
        Assert.Equal(CharacterLevelProgression.GetLevelUpExperience(1), hero.Experience);
        Assert.Equal(2, hero.UnspentStatPoints);
        Assert.Equal(11, hero.GetBaseStat(StatType.Bili));
    }

    [Fact]
    public async Task StoryCommandDispatcher_LevelUp_PromotesCharacterToNextLevel()
    {
        var defaultGrowth = TestContentFactory.CreateGrowTemplate(
            "default",
            new Dictionary<StatType, int>
            {
                [StatType.Gengu] = 2,
            });
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.Gengu] = 8,
            });
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            growTemplates: [defaultGrowth]);
        var state = new GameState();
        var party = new Party();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        party.AddMember(hero);
        state.SetParty(party);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, new RecordingRuntimeHost());
        var publishedEvents = CollectPublishedEvents(session);

        await dispatcher.ExecuteCommandAsync("levelup", [ExprValue.FromString("hero")], default);

        var leveledUp = Assert.Single(publishedEvents.OfType<CharacterLeveledUpEvent>());
        Assert.Equal("hero", leveledUp.CharacterId);
        Assert.Equal(1, leveledUp.OldLevel);
        Assert.Equal(2, leveledUp.NewLevel);
        Assert.Equal(2, hero.Level);
        Assert.Equal(CharacterLevelProgression.GetTotalExperienceRequiredForLevel(2), hero.Experience);
        Assert.Equal(2, hero.UnspentStatPoints);
        Assert.Equal(10, hero.GetBaseStat(StatType.Gengu));
    }

    [Fact]
    public async Task StoryCommandDispatcher_LevelUp_KeepsCurrentLevelOverflowExperience()
    {
        var defaultGrowth = TestContentFactory.CreateGrowTemplate(
            "default",
            new Dictionary<StatType, int>
            {
                [StatType.Bili] = 1,
            });
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.Bili] = 10,
            });
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            growTemplates: [defaultGrowth]);
        var state = new GameState();
        var party = new Party();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        party.AddMember(hero);
        state.SetParty(party);
        var session = new GameSession(state, repository);
        var dispatcher = new StoryCommandDispatcher(session, new RecordingRuntimeHost());

        await dispatcher.ExecuteCommandAsync("get_exp", [ExprValue.FromString("hero"), ExprValue.FromNumber(CharacterLevelProgression.GetLevelUpExperience(1) + 7)], default);
        await dispatcher.ExecuteCommandAsync("levelup", [ExprValue.FromString("hero"), ExprValue.FromNumber(2)], default);

        Assert.Equal(4, hero.Level);
        Assert.Equal(CharacterLevelProgression.GetTotalExperienceRequiredForLevel(4) + 7, hero.Experience);
        Assert.Equal(13, hero.GetBaseStat(StatType.Bili));
        Assert.Equal(6, hero.UnspentStatPoints);
    }

    [Fact]
    public void CharacterService_LevelUp_ReachesMaxLevelForInitialHigherLevelCharacter()
    {
        var defaultGrowth = TestContentFactory.CreateGrowTemplate("default");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero", level: 2);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            growTemplates: [defaultGrowth]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);

        session.CharacterService.LevelUp("hero", 30);

        Assert.Equal(CharacterLevelProgression.MaxLevel, hero.Level);
        Assert.Equal(
            CharacterLevelProgression.GetTotalExperienceRequiredForLevel(CharacterLevelProgression.MaxLevel),
            hero.Experience);
    }

    [Fact]
    public void CharacterService_GainExperience_AppliesDefaultGrowTemplateAndPublishesLevelUpEvent()
    {
        var defaultGrowth = TestContentFactory.CreateGrowTemplate(
            "default",
            new Dictionary<StatType, int>
            {
                [StatType.Bili] = 2,
                [StatType.MaxHp] = 5,
            });
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.Bili] = 10,
                [StatType.MaxHp] = 40,
            });
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            growTemplates: [defaultGrowth]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        var publishedEvents = CollectPublishedEvents(session);

        session.CharacterService.GainExperience("hero", CharacterLevelProgression.GetTotalExperienceRequiredForLevel(3));

        var leveled = Assert.Single(publishedEvents.OfType<CharacterLeveledUpEvent>());
        Assert.Equal("hero", leveled.CharacterId);
        Assert.Equal(1, leveled.OldLevel);
        Assert.Equal(3, leveled.NewLevel);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is CharacterChangedEvent { CharacterId: "hero" });
        Assert.Equal(3, hero.Level);
        Assert.Equal(CharacterLevelProgression.GetTotalExperienceRequiredForLevel(3), hero.Experience);
        Assert.Equal(4, hero.UnspentStatPoints);
        Assert.Equal(14, hero.GetBaseStat(StatType.Bili));
        Assert.Equal(50, hero.GetBaseStat(StatType.MaxHp));
    }

    [Fact]
    public void CharacterLevelProgression_UsesTotalExperienceForDisplayProgress()
    {
        var totalExperience = CharacterLevelProgression.GetTotalExperienceRequiredForLevel(3) + 7;
        var displayProgress = CharacterLevelProgression.GetDisplayProgress(3, totalExperience);

        Assert.Equal(7, displayProgress.CurrentExperience);
        Assert.Equal(CharacterLevelProgression.GetLevelUpExperience(3), displayProgress.NextLevelExperience);
    }

    [Fact]
    public void InventoryService_EquipFromStack_PublishesInventoryAndCharacterChanged()
    {
        var sword = TestContentFactory.CreateEquipment("iron_sword");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            equipment: [sword]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        state.Inventory.AddItem(sword, 1);
        var session = new GameSession(state, repository);
        var publishedEvents = CollectPublishedEvents(session);

        session.InventoryService.EquipFromStack(hero, sword);

        Assert.Collection(
            publishedEvents,
            sessionEvent => Assert.IsType<InventoryChangedEvent>(sessionEvent),
            sessionEvent =>
            {
                var characterChanged = Assert.IsType<CharacterChangedEvent>(sessionEvent);
                Assert.Equal("hero", characterChanged.CharacterId);
            });
    }

    [Fact]
    public void CharacterService_SetExternalSkillActive_PublishesCharacterChangedEvent()
    {
        var strike = TestContentFactory.CreateExternalSkill("strike");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(strike, Level: 3)]);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [strike]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        var publishedEvents = CollectPublishedEvents(session);

        session.CharacterService.SetExternalSkillActive("hero", strike.Id, false);

        var characterChanged = Assert.Single(publishedEvents.OfType<CharacterChangedEvent>());
        Assert.Equal("hero", characterChanged.CharacterId);
        Assert.False(hero.ExternalSkills[0].IsActive);
    }

    [Fact]
    public void CharacterService_SetCharacterPortraitAndModel_PublishesCharacterChangedEvent()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero", portrait: "portrait.old", model: "model.old");
        var repository = TestContentFactory.CreateRepository(characters: [heroDefinition]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        var publishedEvents = CollectPublishedEvents(session);

        session.CharacterService.SetCharacterPortrait("hero", "portrait.new");
        session.CharacterService.SetCharacterModel("hero", "model.new");

        Assert.Equal("portrait.new", hero.Portrait);
        Assert.Equal("model.new", hero.Model);
        Assert.Equal(
            ["hero", "hero"],
            publishedEvents.OfType<CharacterChangedEvent>().Select(evt => evt.CharacterId).ToArray());
    }

    [Fact]
    public void CharacterService_SetSpecialSkillActive_PublishesCharacterChangedEvent()
    {
        var rush = new SpecialSkillDefinition(
            "blood_rush",
            "血战到底",
            "",
            "",
            0,
            SkillCostDefinition.None,
            null,
            "",
            "",
            null,
            []);
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            specialSkills: [rush]);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            specialSkills: [rush]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        var publishedEvents = CollectPublishedEvents(session);

        session.CharacterService.SetSpecialSkillActive("hero", rush.Id, false);

        var characterChanged = Assert.Single(publishedEvents.OfType<CharacterChangedEvent>());
        Assert.Equal("hero", characterChanged.CharacterId);
        Assert.False(hero.SpecialSkills[0].IsActive);
    }

    [Fact]
    public void CharacterService_EquipInternalSkill_RebuildsSnapshotAndPublishesCharacterChangedEvent()
    {
        var guarded = TestContentFactory.CreateInternalSkill(
            "guarded",
            affixes:
            [
                new SkillAffixDefinition(
                    new StatModifierAffix(StatType.Gengu, ModifierValue.Add(6)),
                    RequiresEquippedInternalSkill: true),
            ]);
        var swift = TestContentFactory.CreateInternalSkill("swift");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.Gengu] = 10,
            },
            internalSkills:
            [
                new InitialInternalSkillEntryDefinition(swift, Equipped: true),
                new InitialInternalSkillEntryDefinition(guarded),
            ]);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            internalSkills: [guarded, swift]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        var publishedEvents = CollectPublishedEvents(session);

        session.CharacterService.EquipInternalSkill("hero", guarded.Id);

        var characterChanged = Assert.Single(publishedEvents.OfType<CharacterChangedEvent>());
        Assert.Equal("hero", characterChanged.CharacterId);
        Assert.Equal(guarded.Id, hero.EquippedInternalSkillId);
        Assert.Equal(16, hero.GetStat(StatType.Gengu));
    }

    [Fact]
    public void PartyService_MoveMember_PublishesPartyChangedEvent()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(Party.HeroCharacterId);
        var allyDefinition = TestContentFactory.CreateCharacterDefinition("ally");
        var repository = TestContentFactory.CreateRepository(characters: [heroDefinition, allyDefinition]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance(Party.HeroCharacterId, heroDefinition, state.EquipmentInstanceFactory);
        var ally = TestContentFactory.CreateCharacterInstance("ally", allyDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        state.Party.AddMember(ally);
        var session = new GameSession(state, repository);
        var publishedEvents = CollectPublishedEvents(session);

        session.State.Party.AddMember(TestContentFactory.CreateCharacterInstance("ally_2", allyDefinition, state.EquipmentInstanceFactory));
        publishedEvents.Clear();

        session.PartyService.MoveMember("ally", 2);

        Assert.Single(publishedEvents.OfType<PartyChangedEvent>());
        Assert.Equal([Party.HeroCharacterId, "ally_2", "ally"], session.State.Party.Members.Select(member => member.Id).ToArray());
    }

    [Fact]
    public void LoadSave_PublishesSaveLoadedEvent()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var repository = TestContentFactory.CreateRepository(characters: [heroDefinition]);
        var originalState = new GameState();
        originalState.SetParty(new Party());
        var session = new GameSession(originalState, repository);
        var publishedEvents = CollectPublishedEvents(session);

        var restoredParty = new Party();
        restoredParty.AddMember(TestContentFactory.CreateCharacterInstance("hero", heroDefinition));
        var restoredState = new GameState();
        restoredState.SetParty(restoredParty);
        restoredState.Currency.AddSilver(33);
        restoredState.Location.ChangeMap("home");
        var saveGame = SaveGame.Create(
            restoredState.Adventure,
            restoredState.Party,
            restoredState.Inventory,
            restoredState.Chest,
            restoredState.EquipmentInstanceFactory,
            restoredState.Currency,
            restoredState.Clock,
            restoredState.Location,
            restoredState.MapEventProgress,
            restoredState.WorldTriggers,
            storyState: restoredState.Story);

        session.SaveGameService.LoadSave(saveGame);

        Assert.Single(publishedEvents.OfType<SaveLoadedEvent>());
    }

    private static List<object> CollectPublishedEvents(GameSession session)
    {
        var publishedEvents = new List<object>();
        session.Events.SubscribeAll(publishedEvents.Add);
        return publishedEvents;
    }

    private static EquipmentRandomAffixTableDefinition CreateSpeedAffixTable(string value) =>
        new()
        {
            MinItemLevel = 1,
            MaxItemLevel = 1,
            Options =
            [
                new EquipmentRandomAffixOptionDefinition
                {
                    Kind = EquipmentRandomAffixKind.Speed,
                    Weight = 1,
                    Pool = [value],
                },
            ],
        };

    private sealed class RecordingRuntimeHost : IRuntimeHost
    {
        private readonly Queue<int> _choiceSelections = new();

        public RecordingRuntimeHost(params int[] choiceSelections)
        {
            foreach (var choiceSelection in choiceSelections)
            {
                _choiceSelections.Enqueue(choiceSelection);
            }
        }

        public List<(string Name, IReadOnlyList<ExprValue> Args)> Commands { get; } = [];
        public List<ChoiceContext> Choices { get; } = [];

        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken) =>
            ValueTask.FromException<ExprValue>(new InvalidOperationException($"Unknown variable '{name}'."));

        public ValueTask<bool> EvaluatePredicateAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<bool>(new InvalidOperationException($"Unknown predicate '{name}'."));

        public ValueTask<StoryCommandResult> ExecuteCommandAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken)
        {
            Commands.Add((name, args));
            return ValueTask.FromResult(StoryCommandResult.None);
        }

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken)
        {
            Choices.Add(choice);
            return ValueTask.FromResult(_choiceSelections.TryDequeue(out var selectedIndex) ? selectedIndex : 0);
        }

        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
            ValueTask.FromResult(BattleOutcome.Win);
    }

    private sealed class ThrowingRuntimeHost : IRuntimeHost
    {
        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
            ValueTask.FromException(new InvalidOperationException("Dialogue should not be invoked."));

        public ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken) =>
            ValueTask.FromException<ExprValue>(new InvalidOperationException("Variable lookup should not be invoked."));

        public ValueTask<bool> EvaluatePredicateAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<bool>(new InvalidOperationException("Predicate evaluation should not be invoked."));

        public ValueTask<StoryCommandResult> ExecuteCommandAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<StoryCommandResult>(new InvalidOperationException($"Host command '{name}' should not be invoked."));

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) =>
            ValueTask.FromException<int>(new InvalidOperationException("Choice UI should not be invoked."));

        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
            ValueTask.FromException<BattleOutcome>(new InvalidOperationException("Battle resolution should not be invoked."));
    }
}
