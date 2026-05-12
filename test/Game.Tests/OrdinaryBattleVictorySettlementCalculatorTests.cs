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
    public void BattleServicePreviewOrdinaryVictorySettlement_UsesConfiguredGoldDropChance()
    {
        var state = new GameState();
        var repository = TestContentFactory.CreateRepository();
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

        var settlement = session.BattleService.PreviewOrdinaryVictorySettlement(battleState);

        Assert.Equal(1, settlement.Gold);
    }

    [Fact]
    public void BattleServicePreviewOrdinaryVictorySettlement_UsesConfiguredPlayerTeam()
    {
        var state = new GameState();
        var configuredPlayerDefinition = TestContentFactory.CreateCharacterDefinition("configured_player", level: 1);
        state.Party.AddMember(TestContentFactory.CreateCharacterInstance(
            "configured_player",
            configuredPlayerDefinition,
            state.EquipmentInstanceFactory));
        var repository = TestContentFactory.CreateRepository();
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

        var settlement = session.BattleService.PreviewOrdinaryVictorySettlement(battleState);

        Assert.Equal(
            Math.Max(5, (int)(CharacterLevelProgression.GetLevelUpExperience(10) / 15d)),
            settlement.ExperiencePerMember);
    }

    [Fact]
    public void BattleServicePreviewOrdinaryVictorySettlement_DividesExperienceByRewardEligibleMembersOnly()
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
        var session = new GameSession(state, repository, config: new GameConfig { BattleGoldDropChance = 0d });
        var battleState = new BattleState(
            new BattleGrid(4, 4),
            [
                new BattleUnit("hero", hero, 1, new GridPosition(0, 0)),
                new BattleUnit("fixed_ally", fixedAlly, 1, new GridPosition(0, 1)),
                new BattleUnit("enemy", enemy, 2, new GridPosition(3, 0)),
            ]);

        var settlement = session.BattleService.PreviewOrdinaryVictorySettlement(battleState);

        Assert.Equal(
            Math.Max(5, (int)(CharacterLevelProgression.GetLevelUpExperience(10) / 15d)),
            settlement.ExperiencePerMember);
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
        Assert.Equal(1, state.Currency.Gold);
        Assert.True(state.Inventory.ContainsStack(potion, 2));
        var equipmentEntry = Assert.Single(state.Inventory.Entries.OfType<EquipmentInstanceInventoryEntry>());
        Assert.Equal("青锋剑", equipmentEntry.Equipment.Definition.Name);
        Assert.Single(equipmentEntry.Equipment.ExtraAffixes);
    }

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
            maxHp: maxHp,
            maxMp: maxMp,
            hp: hp);
    }
}
