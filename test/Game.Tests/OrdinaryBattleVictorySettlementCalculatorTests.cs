using Game.Core.Battle;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Application;

namespace Game.Tests;

public sealed class OrdinaryBattleVictorySettlementCalculatorTests
{
    [Fact]
    public void Calculate_UsesLegacyOrdinaryBattleExperienceAndCurrencyFormulas()
    {
        var state = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("hero", team: 1, level: 8, new GridPosition(0, 0)),
                CreateUnit("fallen_ally", team: 1, level: 5, new GridPosition(0, 1), hp: 0),
                CreateUnit("enemy_a", team: 2, level: 6, new GridPosition(3, 0)),
                CreateUnit("enemy_b", team: 2, level: 11, new GridPosition(3, 1)),
            ]);

        var settlement = OrdinaryBattleVictorySettlementCalculator.Calculate(
            state,
            goldDropChance: 0d);

        var expectedExperience = (int)(
            (CharacterLevelProgression.GetLevelUpExperience(6) / 15d +
             CharacterLevelProgression.GetLevelUpExperience(11) / 15d) / 2d);
        var expectedSilver = Math.Max(
            10,
            (int)Math.Pow(1.2d, 6) + (int)Math.Pow(1.2d, 11));

        Assert.Equal(expectedExperience, settlement.ExperiencePerMember);
        Assert.Equal(expectedSilver, settlement.Silver);
        Assert.Equal(0, settlement.Gold);
    }

    [Fact]
    public void Calculate_EnforcesLegacyMinimumRewards()
    {
        var state = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("hero", team: 1, level: 1, new GridPosition(0, 0)),
                CreateUnit("enemy", team: 2, level: 1, new GridPosition(3, 0)),
            ]);

        var settlement = OrdinaryBattleVictorySettlementCalculator.Calculate(
            state,
            goldDropChance: 0d);

        Assert.Equal(5, settlement.ExperiencePerMember);
        Assert.Equal(10, settlement.Silver);
        Assert.Equal(0, settlement.Gold);
    }

    [Fact]
    public void Calculate_AppliesExperienceMultiplierAfterMinimumExperience()
    {
        var state = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("hero", team: 1, level: 1, new GridPosition(0, 0)),
                CreateUnit("enemy", team: 2, level: 1, new GridPosition(3, 0)),
            ]);

        var settlement = OrdinaryBattleVictorySettlementCalculator.Calculate(
            state,
            goldDropChance: 0d,
            experienceMultiplier: 8d);

        Assert.Equal(40, settlement.ExperiencePerMember);
        Assert.Equal(10, settlement.Silver);
        Assert.Equal(0, settlement.Gold);
    }

    [Fact]
    public void Calculate_RollsSingleGoldWhenChanceSucceeds()
    {
        var state = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("hero", team: 1, level: 1, new GridPosition(0, 0)),
                CreateUnit("enemy", team: 2, level: 10, new GridPosition(3, 0)),
            ]);

        var settlement = OrdinaryBattleVictorySettlementCalculator.Calculate(
            state,
            goldDropChance: 1d);

        Assert.Equal(1, settlement.Gold);
    }

    [Fact]
    public void Calculate_RequiresAtLeastOnePlayerUnit()
    {
        var state = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("enemy", team: 2, level: 10, new GridPosition(3, 0)),
            ]);

        Assert.Throws<InvalidOperationException>(() =>
            OrdinaryBattleVictorySettlementCalculator.Calculate(state, goldDropChance: 0.25d));
    }

    [Fact]
    public void BattleServicePreviewVictorySettlement_UsesConfiguredGoldDropChance()
    {
        var state = new GameState();
        var repository = TestContentFactory.CreateRepository(battles: [CreateSettlementBattle()]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                BattleGoldDropChance = 1d,
            });
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("hero", team: 1, level: 1, new GridPosition(0, 0)),
                CreateUnit("enemy", team: 2, level: 10, new GridPosition(3, 0)),
            ]);

        var settlement = PreviewOrdinarySettlement(session, battleState);

        Assert.Equal(1, settlement.Gold);
    }

    [Fact]
    public void BattleServicePreviewVictorySettlement_UsesBattleExperienceMultiplier()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero", level: 1);
        var enemyDefinition = TestContentFactory.CreateCharacterDefinition("enemy", level: 1);
        var battle = new BattleDefinition
        {
            Id = "training_battle",
            Name = "training_battle",
            MapId = "test",
            ExperienceMultiplier = 8d,
        };
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition, enemyDefinition],
            battles: [battle]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance(
            "hero",
            heroDefinition,
            state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var enemy = TestContentFactory.CreateCharacterInstance("enemy", enemyDefinition);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig { BattleGoldDropChance = 0d });
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                new BattleUnit("hero", hero, 1, new GridPosition(0, 0)),
                new BattleUnit("enemy", enemy, 2, new GridPosition(3, 0)),
            ]);

        var settlement = session.BattleService.PreviewVictorySettlement(
            battleState,
            new OrdinaryBattleRequest("training_battle", ["hero"]));

        Assert.Equal(40, settlement.ExperiencePerMember);
    }

    [Fact]
    public void BattleServicePreviewVictorySettlement_UsesConfiguredPlayerTeam()
    {
        var state = new GameState();
        var configuredPlayerDefinition = TestContentFactory.CreateCharacterDefinition("configured_player", level: 1);
        state.Party.AddMember(TestContentFactory.CreateCharacterInstance(
            "configured_player",
            configuredPlayerDefinition,
            state.EquipmentInstanceFactory));
        var repository = TestContentFactory.CreateRepository(battles: [CreateSettlementBattle()]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                BattlePlayerTeam = 2,
                BattleGoldDropChance = 0d,
            });
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("configured_player", team: 2, level: 1, new GridPosition(0, 0)),
                CreateUnit("configured_enemy", team: 1, level: 10, new GridPosition(3, 0)),
            ]);

        var settlement = PreviewOrdinarySettlement(session, battleState);

        Assert.Equal(
            Math.Max(5, (int)(CharacterLevelProgression.GetLevelUpExperience(10) / 15d)),
            settlement.ExperiencePerMember);
    }

    [Fact]
    public void BattleServicePreviewVictorySettlement_DividesExperienceByRewardEligibleMembersOnly()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero", level: 1);
        var allyDefinition = TestContentFactory.CreateCharacterDefinition("fixed_ally", level: 1);
        var enemyDefinition = TestContentFactory.CreateCharacterDefinition("enemy", level: 10);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition, allyDefinition, enemyDefinition],
            battles: [CreateSettlementBattle()]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var fixedAlly = TestContentFactory.CreateCharacterInstance("fixed_ally", allyDefinition);
        var enemy = TestContentFactory.CreateCharacterInstance("enemy", enemyDefinition);
        var session = new GameSession(state, repository, config: new GameConfig { BattleGoldDropChance = 0d });
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                new BattleUnit("hero", hero, 1, new GridPosition(0, 0)),
                new BattleUnit("fixed_ally", fixedAlly, 1, new GridPosition(0, 1)),
                new BattleUnit("enemy", enemy, 2, new GridPosition(3, 0)),
            ]);

        var settlement = PreviewOrdinarySettlement(session, battleState);

        Assert.Equal(
            Math.Max(5, (int)(CharacterLevelProgression.GetLevelUpExperience(10) / 15d)),
            settlement.ExperiencePerMember);
    }

    [Fact]
    public void BattleServicePreviewVictorySettlement_NormalDifficultyDoesNotDropFragments()
    {
        var externalSkill = TestContentFactory.CreateExternalSkill("dragon_palm", hard: 1d);
        var internalSkill = TestContentFactory.CreateInternalSkill("yijinjing", hard: 1d);
        var repository = TestContentFactory.CreateRepository(
            externalSkills: [externalSkill],
            internalSkills: [internalSkill],
            battles: [CreateSettlementBattle()]);
        var state = new GameState();
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                OrdinaryBattleDropChance = 0d,
                HardModeCanzhangDropRate = 1d,
                CrazyModeCanzhangDropRate = 1d,
            });
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("hero", team: 1, level: 1, new GridPosition(0, 0)),
                CreateUnit("enemy", team: 2, level: 10, new GridPosition(3, 0)),
            ]);

        var settlement = PreviewOrdinarySettlement(session, battleState);

        Assert.Empty(settlement.Drops.OfType<OrdinaryBattleSkillFragmentRewardDrop>());
    }

    [Fact]
    public void BattleServicePreviewVictorySettlement_HardDifficultyDropsFilteredExternalFragments()
    {
        var low = TestContentFactory.CreateExternalSkill("too_low", hard: 2d);
        var eligible = TestContentFactory.CreateExternalSkill("eligible", hard: 5d);
        var tooHard = TestContentFactory.CreateExternalSkill("too_hard", hard: 8d);
        var internalSkill = TestContentFactory.CreateInternalSkill("internal", hard: 1d);
        var repository = TestContentFactory.CreateRepository(
            externalSkills: [low, eligible, tooHard],
            internalSkills: [internalSkill],
            battles: [CreateSettlementBattle()]);
        var state = new GameState();
        state.Adventure.SetDifficulty(GameDifficulty.Hard);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                OrdinaryBattleDropChance = 0d,
                HardModeCanzhangDropRate = 1d,
                CanzhangDropRateInternalRate = double.PositiveInfinity,
                CanzhangMaxHardSkill = 8d,
                MaxLevel = 30,
            });
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("hero", team: 1, level: 1, new GridPosition(0, 0)),
                CreateUnit("enemy", team: 2, level: 30, new GridPosition(3, 0)),
            ]);

        var settlement = PreviewOrdinarySettlement(session, battleState);

        var fragment = Assert.Single(settlement.Drops.OfType<OrdinaryBattleSkillFragmentRewardDrop>());
        Assert.Equal(SkillFragmentKind.External, fragment.Kind);
        Assert.Equal("eligible", fragment.SkillId);
        Assert.Equal("eligible残章", fragment.DisplayName);
    }

    [Fact]
    public void BattleServicePreviewVictorySettlement_CrazyDifficultyDropsInternalFragmentsWithRoundScaling()
    {
        var lowExternal = TestContentFactory.CreateExternalSkill("too_hard_external", hard: 9d);
        var internalSkill = TestContentFactory.CreateInternalSkill("eligible_internal", hard: 2d);
        var repository = TestContentFactory.CreateRepository(
            externalSkills: [lowExternal],
            internalSkills: [internalSkill],
            battles: [CreateSettlementBattle()]);
        var state = new GameState();
        state.Adventure.SetDifficulty(GameDifficulty.Crazy);
        state.Adventure.SetRound(3);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                OrdinaryBattleDropChance = 0d,
                CrazyModeCanzhangDropRate = 0d,
                CrazyModeCanzhangDropRatePerRound = 0.5d,
                CanzhangDropRateInternalRate = 1d,
                CanzhangMaxHardInternalSkill = 8d,
            });
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("hero", team: 1, level: 1, new GridPosition(0, 0)),
                CreateUnit("enemy", team: 2, level: 10, new GridPosition(3, 0)),
            ]);

        var settlement = PreviewOrdinarySettlement(session, battleState);

        var fragment = Assert.Single(settlement.Drops.OfType<OrdinaryBattleSkillFragmentRewardDrop>());
        Assert.Equal(SkillFragmentKind.Internal, fragment.Kind);
        Assert.Equal("eligible_internal", fragment.SkillId);
    }

    [Fact]
    public void BattleServicePreviewVictorySettlement_NoFragmentCandidatesDoesNotThrow()
    {
        var externalSkill = TestContentFactory.CreateExternalSkill("too_hard", hard: 9d);
        var internalSkill = TestContentFactory.CreateInternalSkill("also_too_hard", hard: 9d);
        var repository = TestContentFactory.CreateRepository(
            externalSkills: [externalSkill],
            internalSkills: [internalSkill],
            battles: [CreateSettlementBattle()]);
        var state = new GameState();
        state.Adventure.SetDifficulty(GameDifficulty.Hard);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                OrdinaryBattleDropChance = 0d,
                HardModeCanzhangDropRate = 1d,
                CanzhangDropRateInternalRate = 1d,
            });
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                CreateUnit("hero", team: 1, level: 1, new GridPosition(0, 0)),
                CreateUnit("enemy", team: 2, level: 10, new GridPosition(3, 0)),
            ]);

        var settlement = PreviewOrdinarySettlement(session, battleState);

        Assert.Empty(settlement.Drops.OfType<OrdinaryBattleSkillFragmentRewardDrop>());
    }

    [Fact]
    public void BattleServiceApplyOrdinaryVictorySettlement_IgnoresFixedPlayerTeamNpcExperience()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero", level: 1);
        var allyDefinition = TestContentFactory.CreateCharacterDefinition("fixed_ally", level: 1);
        var enemyDefinition = TestContentFactory.CreateCharacterDefinition("enemy", level: 10);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition, allyDefinition, enemyDefinition]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var fixedAlly = TestContentFactory.CreateCharacterInstance("fixed_ally", allyDefinition);
        var enemy = TestContentFactory.CreateCharacterInstance("enemy", enemyDefinition);
        var session = new GameSession(state, repository);
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                new BattleUnit("hero", hero, 1, new GridPosition(0, 0)),
                new BattleUnit("fixed_ally", fixedAlly, 1, new GridPosition(0, 1)),
                new BattleUnit("enemy", enemy, 2, new GridPosition(3, 0)),
            ]);
        var settlement = new OrdinaryBattleVictorySettlement(5, 0, 0);

        session.BattleService.ApplyOrdinaryVictorySettlement(battleState, settlement);

        Assert.Equal(5, hero.Experience);
        Assert.Equal(0, fixedAlly.Experience);
    }

    [Fact]
    public void BattleServiceApplyOrdinaryVictorySettlement_AppliesFragmentsToProfileOnlyAndPublishesOnce()
    {
        var externalSkill = TestContentFactory.CreateExternalSkill("dragon_palm");
        var internalSkill = TestContentFactory.CreateInternalSkill("yijinjing");
        var repository = TestContentFactory.CreateRepository(
            externalSkills: [externalSkill],
            internalSkills: [internalSkill]);
        var state = new GameState();
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero", level: 1);
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        var profileChangedCount = 0;
        var inventoryChangedCount = 0;
        using var profileSubscription = session.Events.Subscribe<ProfileChangedEvent>(_ => profileChangedCount++);
        using var inventorySubscription = session.Events.Subscribe<InventoryChangedEvent>(_ => inventoryChangedCount++);
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                new BattleUnit("hero", hero, 1, new GridPosition(0, 0)),
                CreateUnit("enemy", team: 2, level: 10, new GridPosition(3, 0)),
            ]);
        var settlement = new OrdinaryBattleVictorySettlement(
            5,
            0,
            0,
            [
                new OrdinaryBattleSkillFragmentRewardDrop(
                    SkillFragmentKind.External,
                    externalSkill.Id,
                    $"{externalSkill.Name}残章"),
                new OrdinaryBattleSkillFragmentRewardDrop(
                    SkillFragmentKind.Internal,
                    internalSkill.Id,
                    $"{internalSkill.Name}残章"),
            ]);

        session.BattleService.ApplyOrdinaryVictorySettlement(battleState, settlement);

        Assert.Equal(1, session.Profile.GetSkillMaxLevelBonus(externalSkill.Id));
        Assert.Equal(1, session.Profile.GetSkillMaxLevelBonus(internalSkill.Id));
        Assert.Empty(state.Inventory.Entries);
        Assert.Equal(1, profileChangedCount);
        Assert.Equal(0, inventoryChangedCount);
    }

    [Fact]
    public void GenerateEquipmentRolls_AttackComboProducesAttackAndCritChanceAffixes()
    {
        var equipment = TestContentFactory.CreateEquipment("测试武器", level: 4);
        var repository = TestContentFactory.CreateRepository(
            equipmentRandomAffixTables:
            [
                new EquipmentRandomAffixTableDefinition
                {
                    MinItemLevel = 1,
                    MaxItemLevel = 7,
                    Options =
                    [
                        new EquipmentRandomAffixOptionDefinition
                        {
                            Kind = EquipmentRandomAffixKind.AttackCombo,
                            Weight = 1,
                        },
                    ],
                },
            ]);

        var rolls = OrdinaryBattleLootGenerator.GenerateEquipmentRolls(equipment, repository, round: 1);

        Assert.InRange(rolls.Count, 1, 4);
        Assert.All(rolls, roll =>
        {
            Assert.Equal(EquipmentRandomAffixKind.AttackCombo, roll.Kind);
            Assert.Collection(
                roll.Affixes,
                affix => Assert.Equal(StatType.Attack, Assert.IsType<Game.Core.Affix.StatModifierAffix>(affix).Stat),
                affix => Assert.Equal(StatType.CritChance, Assert.IsType<Game.Core.Affix.StatModifierAffix>(affix).Stat));
        });
    }

    [Fact]
    public void GenerateEquipmentRolls_DefenceComboProducesDefenceAndAntiCritChanceAffixes()
    {
        var equipment = TestContentFactory.CreateEquipment("测试护甲", slotType: EquipmentSlotType.Armor, level: 4);
        var repository = TestContentFactory.CreateRepository(
            equipmentRandomAffixTables:
            [
                new EquipmentRandomAffixTableDefinition
                {
                    MinItemLevel = 1,
                    MaxItemLevel = 7,
                    Options =
                    [
                        new EquipmentRandomAffixOptionDefinition
                        {
                            Kind = EquipmentRandomAffixKind.DefenceCombo,
                            Weight = 1,
                            Ranges =
                            [
                                new EquipmentRandomAffixRangeDefinition(8, 15),
                                new EquipmentRandomAffixRangeDefinition(0, 5),
                            ],
                        },
                    ],
                },
            ]);

        var rolls = OrdinaryBattleLootGenerator.GenerateEquipmentRolls(equipment, repository, round: 1);

        Assert.InRange(rolls.Count, 1, 4);
        Assert.All(rolls, roll =>
        {
            Assert.Equal(EquipmentRandomAffixKind.DefenceCombo, roll.Kind);
            Assert.Collection(
                roll.Affixes,
                affix => Assert.Equal(StatType.Defence, Assert.IsType<Game.Core.Affix.StatModifierAffix>(affix).Stat),
                affix => Assert.Equal(StatType.AntiCritChance, Assert.IsType<Game.Core.Affix.StatModifierAffix>(affix).Stat));
        });
    }

    [Fact]
    public void BattleServiceApplyOrdinaryVictorySettlement_AppliesExperienceCurrencyAndDrops()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero", level: 1);
        var enemyDefinition = TestContentFactory.CreateCharacterDefinition("enemy", level: 10);
        var potion = new NormalItemDefinition
        {
            Id = "healing_potion",
            Name = "疗伤药",
            Type = ItemType.Consumable,
        };
        var equipment = TestContentFactory.CreateEquipment("青锋剑", level: 4);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition, enemyDefinition],
            equipment: [equipment],
            items: [potion]);
        var state = new GameState();
        var hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, repository);
        var enemy = TestContentFactory.CreateCharacterInstance("enemy", enemyDefinition);
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                new BattleUnit("hero", hero, 1, new GridPosition(0, 0)),
                new BattleUnit("enemy", enemy, 2, new GridPosition(3, 0)),
            ]);
        var settlement = new OrdinaryBattleVictorySettlement(
            5,
            23,
            1,
            [
                new OrdinaryBattleStackRewardDrop(potion, 2),
                new OrdinaryBattleEquipmentRewardDrop(
                    equipment,
                    [
                        new GeneratedEquipmentAffixRoll(
                            "attack_combo",
                            EquipmentRandomAffixKind.AttackCombo,
                            [new StatModifierAffix(StatType.Attack, ModifierValue.Add(12))]),
                    ]),
            ]);

        session.BattleService.ApplyOrdinaryVictorySettlement(battleState, settlement);

        Assert.Equal(5, hero.Experience);
        Assert.Equal(23, state.Currency.Silver);
        Assert.Equal(1, session.Profile.Yuanbao);
        Assert.True(state.Inventory.ContainsStack(potion, 2));
        var equipmentEntry = Assert.Single(state.Inventory.Entries.OfType<EquipmentInstanceInventoryEntry>());
        Assert.Equal("青锋剑", equipmentEntry.Equipment.Definition.Name);
        Assert.Single(equipmentEntry.Equipment.ExtraAffixes);
    }

    private static OrdinaryBattleVictorySettlement PreviewOrdinarySettlement(
        GameSession session,
        BattleState state) =>
        session.BattleService.PreviewVictorySettlement(
            state,
            new OrdinaryBattleRequest("settlement", []));

    private static BattleDefinition CreateSettlementBattle() =>
        new() { Id = "settlement", Name = "settlement", MapId = "test" };

    private static BattleUnit CreateUnit(
        string id,
        int team,
        int level,
        GridPosition position,
        int maxHp = 100,
        int maxMp = 30,
        int? hp = null)
    {
        var definition = TestContentFactory.CreateCharacterDefinition(
            id,
            level: level,
            stats: new Dictionary<StatType, int>
            {
                [StatType.MaxHp] = maxHp,
                [StatType.MaxMp] = maxMp,
            },
            externalSkills: Array.Empty<InitialExternalSkillEntryDefinition>());
        var character = TestContentFactory.CreateCharacterInstance(id, definition);

        return new BattleUnit(
            id,
            character,
            team,
            position,
            hp: hp);
    }
}
