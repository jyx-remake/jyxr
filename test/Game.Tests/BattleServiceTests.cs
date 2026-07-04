using Game.Application;
using Game.Core.Definitions;
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
            new GameConfig
            {
                EnemyRandomTalentIds = [],
                EnemyRandomTalentCrazy1Ids = [],
                EnemyRandomTalentCrazy2Ids = [],
                EnemyRandomTalentCrazy3Ids = [],
            });

        var state = session.BattleService.BuildZhenlongqijuBattleState(
            session.ContentRepository.GetBattle("fixed_player"),
            [],
            level: 0);

        Assert.Equal(GameDifficulty.Crazy, state.RuleSettings.Difficulty);
        Assert.True(state.RuleSettings.EnableDifficultyDamageScaling);
        Assert.True(state.RuleSettings.EnableDifficultyItemCooldownRules);
        Assert.False(state.RuleSettings.EnableRoundEnemyAttackDefenceScaling);
    }

    private static GameSession CreateSession(BattleDefinition battle, GameConfig? config = null)
    {
        var shadow = TestContentFactory.CreateCharacterDefinition("shadow");
        var enemy = TestContentFactory.CreateCharacterDefinition("enemy");
        var repository = TestContentFactory.CreateRepository(
            characters: [shadow, enemy],
            battles: [battle]);
        return new GameSession(new GameState(), repository, config: config);
    }

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
