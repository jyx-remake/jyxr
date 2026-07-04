using Game.Application;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Tests;

public sealed class BattleServiceTests
{
    private const string ZhenlongWeaponId = "zhenlong_weapon";
    private const string ZhenlongArmorId = "zhenlong_armor";
    private const string ZhenlongAccessoryId = "zhenlong_accessory";

    [Fact]
    public void BuildBattleState_AllowsFixedPlayerBattleWithoutSelectedCharacters()
    {
        var session = CreateSession(CreateFixedPlayerBattle());

        var state = session.BattleService.BuildBattleState("fixed_player", []);

        Assert.Contains(state.Units, unit => unit.Team == 1 && unit.Character.Definition.Id == "shadow");
        Assert.Contains(state.Units, unit => unit.Team == 2 && unit.Character.Definition.Id == "enemy");
    }

    [Fact]
    public void BuildBattleState_Throws_WhenBattleHasNoPlayerTeamUnit()
    {
        var session = CreateSession(CreateEnemyOnlyBattle());

        var exception = Assert.Throws<InvalidOperationException>(
            () => session.BattleService.BuildBattleState("enemy_only", []));
        Assert.Contains("enemy_only", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildZhenlongqijuBattleState_UsesCrazyBattleDifficulty()
    {
        var session = CreateSession(
            CreateFixedPlayerBattle(),
            CreateConfigWithoutEnemyRandomTalents());

        var state = session.BattleService.BuildZhenlongqijuBattleState(
            session.ContentRepository.GetBattle("fixed_player"),
            [],
            level: 0);

        Assert.Equal(GameDifficulty.Crazy, state.RuleSettings.Difficulty);
        Assert.True(state.RuleSettings.EnableDifficultyDamageScaling);
        Assert.True(state.RuleSettings.EnableDifficultyItemCooldownRules);
        Assert.False(state.RuleSettings.EnableRoundEnemyAttackDefenceScaling);
    }

    [Theory]
    [InlineData(5, 100, 120, 10100, 10120)]
    [InlineData(10, 100, 120, 20200, 20240)]
    public void BuildZhenlongqijuBattleState_AppliesLegacyHpMpFormula(
        int level,
        int baseHp,
        int baseMp,
        int expectedHp,
        int expectedMp)
    {
        var enemy = TestContentFactory.CreateCharacterDefinition(
            "enemy",
            stats: new Dictionary<StatType, int>
            {
                [StatType.MaxHp] = baseHp,
                [StatType.MaxMp] = baseMp,
            });
        var session = CreateSession(
            CreateFixedPlayerBattle(),
            CreateConfigWithoutEnemyRandomTalents(),
            enemyDefinition: enemy);

        var state = session.BattleService.BuildZhenlongqijuBattleState(
            session.ContentRepository.GetBattle("fixed_player"),
            [],
            level);
        var poweredEnemy = Assert.Single(state.Units.Where(unit => unit.Team == 2)).Character;

        Assert.Equal(expectedHp, poweredEnemy.GetBaseStat(StatType.MaxHp));
        Assert.Equal(expectedMp, poweredEnemy.GetBaseStat(StatType.MaxMp));
    }

    [Fact]
    public void BuildZhenlongqijuBattleState_RollsSkillLevelBonusPerSkill()
    {
        var externalSkills = Enumerable.Range(0, 20)
            .Select(index => TestContentFactory.CreateExternalSkill($"external_{index}"))
            .ToArray();
        var internalSkills = Enumerable.Range(0, 20)
            .Select(index => TestContentFactory.CreateInternalSkill($"internal_{index}"))
            .ToArray();
        var enemy = TestContentFactory.CreateCharacterDefinition(
            "enemy",
            externalSkills: externalSkills
                .Select(skill => new InitialExternalSkillEntryDefinition(skill, Level: 1))
                .ToArray(),
            internalSkills: internalSkills
                .Select(skill => new InitialInternalSkillEntryDefinition(skill, Level: 1))
                .ToArray());
        var session = CreateSession(
            CreateFixedPlayerBattle(),
            CreateConfigWithoutEnemyRandomTalents(),
            enemyDefinition: enemy);

        var state = session.BattleService.BuildZhenlongqijuBattleState(
            session.ContentRepository.GetBattle("fixed_player"),
            [],
            level: 15);
        var poweredEnemy = Assert.Single(state.Units.Where(unit => unit.Team == 2)).Character;
        var bonuses = poweredEnemy.ExternalSkills.Select(skill => skill.Level - 1)
            .Concat(poweredEnemy.InternalSkills.Select(skill => skill.Level - 1))
            .ToArray();

        Assert.All(bonuses, bonus => Assert.InRange(bonus, 3, 5));
        Assert.True(bonuses.Distinct().Count() > 1);
    }

    [Fact]
    public void BuildZhenlongqijuBattleState_ReplacesEnemyEquipmentWithConfiguredRandomSet()
    {
        var oldWeapon = TestContentFactory.CreateEquipment("old_weapon", EquipmentSlotType.Weapon);
        var enemy = TestContentFactory.CreateCharacterDefinition(
            "enemy",
            equipment: [oldWeapon]);
        var session = CreateSession(
            CreateFixedPlayerBattle(),
            CreateConfigWithoutEnemyRandomTalents(),
            enemyDefinition: enemy,
            equipment: [oldWeapon]);

        var state = session.BattleService.BuildZhenlongqijuBattleState(
            session.ContentRepository.GetBattle("fixed_player"),
            [],
            level: 1);
        var poweredEnemy = Assert.Single(state.Units.Where(unit => unit.Team == 2)).Character;

        Assert.Equal(
            [EquipmentSlotType.Weapon, EquipmentSlotType.Armor, EquipmentSlotType.Accessory],
            poweredEnemy.EquippedItems.Keys.OrderBy(static slot => slot).ToArray());
        Assert.Equal(ZhenlongWeaponId, poweredEnemy.GetEquipment(EquipmentSlotType.Weapon)?.Definition.Id);
        Assert.Equal(ZhenlongArmorId, poweredEnemy.GetEquipment(EquipmentSlotType.Armor)?.Definition.Id);
        Assert.Equal(ZhenlongAccessoryId, poweredEnemy.GetEquipment(EquipmentSlotType.Accessory)?.Definition.Id);
        Assert.DoesNotContain(
            poweredEnemy.EquippedItems.Values,
            equipment => string.Equals(equipment.Definition.Id, oldWeapon.Id, StringComparison.Ordinal));
        Assert.All(poweredEnemy.EquippedItems.Values, equipment => Assert.Equal(4, equipment.ExtraAffixes.Count));
    }

    [Theory]
    [InlineData(EquipmentSlotType.Weapon)]
    [InlineData(EquipmentSlotType.Armor)]
    [InlineData(EquipmentSlotType.Accessory)]
    public void BuildZhenlongqijuBattleState_Throws_WhenConfiguredEnemyEquipmentSlotPoolIsEmpty(
        EquipmentSlotType slotType)
    {
        var config = CreateConfigWithoutEnemyRandomTalents(
            weaponIds: slotType == EquipmentSlotType.Weapon ? [] : null,
            armorIds: slotType == EquipmentSlotType.Armor ? [] : null,
            accessoryIds: slotType == EquipmentSlotType.Accessory ? [] : null);
        var session = CreateSession(CreateFixedPlayerBattle(), config);

        var exception = Assert.Throws<InvalidOperationException>(
            () => session.BattleService.BuildZhenlongqijuBattleState(
                session.ContentRepository.GetBattle("fixed_player"),
                [],
                level: 1));

        Assert.Contains("Zhenlongqiju enemy equipment", exception.Message, StringComparison.Ordinal);
        Assert.Contains(slotType.ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(EquipmentSlotType.Weapon, EquipmentSlotType.Armor)]
    [InlineData(EquipmentSlotType.Armor, EquipmentSlotType.Accessory)]
    [InlineData(EquipmentSlotType.Accessory, EquipmentSlotType.Weapon)]
    public void BuildZhenlongqijuBattleState_Throws_WhenConfiguredEnemyEquipmentSlotTypeMismatches(
        EquipmentSlotType expectedSlotType,
        EquipmentSlotType actualSlotType)
    {
        var wrongEquipment = TestContentFactory.CreateEquipment("wrong_slot_equipment", actualSlotType);
        var wrongIds = new[] { wrongEquipment.Id };
        var config = CreateConfigWithoutEnemyRandomTalents(
            weaponIds: expectedSlotType == EquipmentSlotType.Weapon ? wrongIds : null,
            armorIds: expectedSlotType == EquipmentSlotType.Armor ? wrongIds : null,
            accessoryIds: expectedSlotType == EquipmentSlotType.Accessory ? wrongIds : null);
        var session = CreateSession(
            CreateFixedPlayerBattle(),
            config,
            equipment: [wrongEquipment]);

        var exception = Assert.Throws<InvalidOperationException>(
            () => session.BattleService.BuildZhenlongqijuBattleState(
                session.ContentRepository.GetBattle("fixed_player"),
                [],
                level: 1));

        Assert.Contains(wrongEquipment.Id, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expectedSlotType.ToString(), exception.Message, StringComparison.Ordinal);
    }

    private static GameSession CreateSession(
        BattleDefinition battle,
        GameConfig? config = null,
        CharacterDefinition? shadowDefinition = null,
        CharacterDefinition? enemyDefinition = null,
        IReadOnlyList<EquipmentDefinition>? equipment = null)
    {
        var shadow = shadowDefinition ?? TestContentFactory.CreateCharacterDefinition("shadow");
        var enemy = enemyDefinition ?? TestContentFactory.CreateCharacterDefinition("enemy");
        var repository = TestContentFactory.CreateRepository(
            characters: [shadow, enemy],
            equipment: CreateZhenlongEquipment().Concat(equipment ?? []).ToArray(),
            equipmentRandomAffixTables: [CreateZhenlongEquipmentAffixTable()],
            battles: [battle]);
        return new GameSession(new GameState(), repository, config: config);
    }

    private static GameConfig CreateConfigWithoutEnemyRandomTalents(
        IReadOnlyList<string>? weaponIds = null,
        IReadOnlyList<string>? armorIds = null,
        IReadOnlyList<string>? accessoryIds = null) =>
        new()
        {
            EnemyRandomTalentIds = [],
            EnemyRandomTalentCrazy1Ids = [],
            EnemyRandomTalentCrazy2Ids = [],
            EnemyRandomTalentCrazy3Ids = [],
            ZhenlongWeaponRewardIds = (weaponIds ?? [ZhenlongWeaponId]).ToList(),
            ZhenlongArmorRewardIds = (armorIds ?? [ZhenlongArmorId]).ToList(),
            ZhenlongAccessoryRewardIds = (accessoryIds ?? [ZhenlongAccessoryId]).ToList(),
        };

    private static IReadOnlyList<EquipmentDefinition> CreateZhenlongEquipment() =>
        [
            TestContentFactory.CreateEquipment(ZhenlongWeaponId, EquipmentSlotType.Weapon),
            TestContentFactory.CreateEquipment(ZhenlongArmorId, EquipmentSlotType.Armor),
            TestContentFactory.CreateEquipment(ZhenlongAccessoryId, EquipmentSlotType.Accessory),
        ];

    private static EquipmentRandomAffixTableDefinition CreateZhenlongEquipmentAffixTable() =>
        new()
        {
            MinItemLevel = 1,
            MaxItemLevel = 99,
            Options =
            [
                new EquipmentRandomAffixOptionDefinition
                {
                    Kind = EquipmentRandomAffixKind.Accuracy,
                    Weight = 1,
                    Ranges = [new EquipmentRandomAffixRangeDefinition(1, 1)],
                },
                new EquipmentRandomAffixOptionDefinition
                {
                    Kind = EquipmentRandomAffixKind.CritMult,
                    Weight = 1,
                    Ranges = [new EquipmentRandomAffixRangeDefinition(1, 1)],
                },
                new EquipmentRandomAffixOptionDefinition
                {
                    Kind = EquipmentRandomAffixKind.Lifesteal,
                    Weight = 1,
                    Ranges = [new EquipmentRandomAffixRangeDefinition(1, 1)],
                },
                new EquipmentRandomAffixOptionDefinition
                {
                    Kind = EquipmentRandomAffixKind.AntiDebuff,
                    Weight = 1,
                    Ranges = [new EquipmentRandomAffixRangeDefinition(1, 1)],
                },
            ],
        };

    private static BattleDefinition CreateFixedPlayerBattle() =>
        new()
        {
            Id = "fixed_player",
            Name = "fixed_player",
            MapId = "test",
            Participants =
            [
                CreateParticipant(team: 1, x: 1, y: 1, characterId: "shadow"),
                CreateParticipant(team: 2, x: 2, y: 1, characterId: "enemy"),
            ],
        };

    private static BattleDefinition CreateEnemyOnlyBattle() =>
        new()
        {
            Id = "enemy_only",
            Name = "enemy_only",
            MapId = "test",
            Participants =
            [
                CreateParticipant(team: 2, x: 2, y: 1, characterId: "enemy"),
            ],
        };

    private static BattleParticipantDefinition CreateParticipant(
        int team,
        int x,
        int y,
        string characterId) =>
        new()
        {
            Position = new GridPosition(x, y),
            Team = team,
            Facing = team == 1 ? 1 : 0,
            CharacterId = characterId,
        };
}
