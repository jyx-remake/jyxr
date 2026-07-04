using Game.Application;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Tests;

public sealed class BattleServiceTests
{
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

    private static GameSession CreateSession(
        BattleDefinition battle,
        GameConfig? config = null,
        CharacterDefinition? shadowDefinition = null,
        CharacterDefinition? enemyDefinition = null)
    {
        var shadow = shadowDefinition ?? TestContentFactory.CreateCharacterDefinition("shadow");
        var enemy = enemyDefinition ?? TestContentFactory.CreateCharacterDefinition("enemy");
        var repository = TestContentFactory.CreateRepository(
            characters: [shadow, enemy],
            battles: [battle]);
        return new GameSession(new GameState(), repository, config: config);
    }

    private static GameConfig CreateConfigWithoutEnemyRandomTalents() =>
        new()
        {
            EnemyRandomTalentIds = [],
            EnemyRandomTalentCrazy1Ids = [],
            EnemyRandomTalentCrazy2Ids = [],
            EnemyRandomTalentCrazy3Ids = [],
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
