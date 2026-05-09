using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Tests;

public sealed class BattleAiTests
{
    [Fact]
    public void Decide_DoesNotChooseSkillThatOnlyHitsSelf()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "self_plus",
            powerBase: 10,
            impactType: SkillImpactType.Plus,
            impactSize: 1,
            castSize: 0);
        var enemy = CreateUnit(
            "enemy",
            team: 2,
            new GridPosition(1, 1),
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var player = CreateUnit("player", team: 1, new GridPosition(4, 4));
        enemy.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [enemy, player]);
        var engine = new BattleEngine();
        engine.BeginAction(state, enemy.Id);
        var agent = new BasicEnemyBattleAgent(new BattleTurnCandidateGenerator(engine));

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.Rest, plan.MainAction.Kind);
    }

    [Fact]
    public void Decide_PrefersLowerFriendlyFireWhenEnemyDamageIsEqual()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "cross_blast",
            powerBase: 10,
            impactType: SkillImpactType.Plus,
            impactSize: 1,
            castSize: 3);
        var enemy = CreateUnit(
            "enemy",
            team: 2,
            new GridPosition(2, 2),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var ally = CreateUnit("ally", team: 2, new GridPosition(3, 2), maxHp: 500);
        var player = CreateUnit("player", team: 1, new GridPosition(4, 2), maxHp: 500);
        enemy.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [enemy, ally, player]);
        var engine = new BattleEngine();
        engine.BeginAction(state, enemy.Id);
        var agent = new BasicEnemyBattleAgent(new BattleTurnCandidateGenerator(engine));

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.CastSkill, plan.MainAction.Kind);
        Assert.Equal(new GridPosition(4, 1), plan.MainAction.TargetPosition);
    }

    private static BattleUnit CreateUnit(
        string id,
        int team,
        GridPosition position,
        int maxHp = 100,
        IReadOnlyDictionary<StatType, int>? stats = null,
        IReadOnlyList<InitialExternalSkillEntryDefinition>? externalSkills = null)
    {
        var mergedStats = new Dictionary<StatType, int>
        {
            [StatType.MaxHp] = maxHp,
            [StatType.MaxMp] = 30,
        };
        foreach (var (stat, value) in stats ?? new Dictionary<StatType, int>())
        {
            mergedStats[stat] = value;
        }

        var definition = TestContentFactory.CreateCharacterDefinition(id, stats: mergedStats, externalSkills: externalSkills);
        var character = TestContentFactory.CreateCharacterInstance(id, definition);
        return new BattleUnit(id, character, team, position, maxHp: maxHp, maxMp: 30);
    }
}
