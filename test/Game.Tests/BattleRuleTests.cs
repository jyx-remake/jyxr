using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Tests;

public sealed class BattleRuleTests
{
    [Fact]
    public void CastSkill_DoesNotTargetSelf_WhenImpactCoversCaster()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "whirlwind",
            powerBase: 10,
            impactType: SkillImpactType.Plus,
            impactSize: 1,
            castSize: 0);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(1, 1),
            maxHp: 500,
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var engine = new BattleEngine(new BattleDamageCalculator(new FixedRandomService(0.5d)));
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), hero.Position);

        Assert.True(result.Success);
        Assert.Equal(500, hero.Hp);
        Assert.Empty(result.AffectedUnitIds);
        Assert.Contains(state.Events, battleEvent => battleEvent.Kind == BattleEventKind.SkillCast);
        Assert.DoesNotContain(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.Damaged &&
            battleEvent.UnitId == hero.Id);
    }

    [Fact]
    public void CastSkill_AppliesQuarterDamageToFriendlyUnits()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "line_strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var stats = new Dictionary<StatType, int>
        {
            [StatType.Quanzhang] = 100,
            [StatType.Bili] = 120,
        };
        var enemySource = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: stats,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(2, 0), maxHp: 500);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        enemySource.ActionGauge = 100;

        var enemyState = new BattleState(new BattleGrid(4, 4), [enemySource, enemy]);
        var enemyEngine = new BattleEngine(calculator);
        enemyEngine.BeginAction(enemyState, enemySource.Id);
        enemyEngine.CastSkill(enemyState, enemySource.Id, enemySource.Character.GetExternalSkills().Single(), enemy.Position);
        var enemyDamage = 500 - enemy.Hp;

        var allySource = CreateUnit(
            "source_ally",
            team: 1,
            new GridPosition(0, 0),
            stats: stats,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var ally = CreateUnit("ally", team: 1, new GridPosition(1, 0), maxHp: 500);
        allySource.ActionGauge = 100;
        var allyState = new BattleState(new BattleGrid(4, 4), [allySource, ally]);
        var allyEngine = new BattleEngine(calculator);
        allyEngine.BeginAction(allyState, allySource.Id);
        var result = allyEngine.CastSkill(allyState, allySource.Id, allySource.Character.GetExternalSkills().Single(), ally.Position);
        var allyDamage = 500 - ally.Hp;

        Assert.True(result.Success);
        Assert.Equal((int)Math.Floor(enemyDamage * BattleDamageRules.FriendlyFireDamageMultiplier), allyDamage);
    }

    private static BattleUnit CreateUnit(
        string id,
        int team,
        GridPosition position,
        int maxHp = 100,
        int maxMp = 30,
        IReadOnlyDictionary<StatType, int>? stats = null,
        IReadOnlyList<InitialExternalSkillEntryDefinition>? externalSkills = null)
    {
        var mergedStats = new Dictionary<StatType, int>
        {
            [StatType.MaxHp] = maxHp,
            [StatType.MaxMp] = maxMp,
        };
        foreach (var (stat, value) in stats ?? new Dictionary<StatType, int>())
        {
            mergedStats[stat] = value;
        }

        var definition = TestContentFactory.CreateCharacterDefinition(id, stats: mergedStats, externalSkills: externalSkills);
        var character = TestContentFactory.CreateCharacterInstance(id, definition);
        return new BattleUnit(id, character, team, position, maxHp: maxHp, maxMp: maxMp);
    }

    private sealed class FixedRandomService : IRandomService
    {
        private readonly double _value;

        public FixedRandomService(double value)
        {
            _value = value;
        }

        public double NextDouble() => _value;

        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
