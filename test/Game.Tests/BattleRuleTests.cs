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
    public void BattleState_DefaultsToNeutralDamageRules()
    {
        var unit = CreateUnit("hero", team: 1, new GridPosition(0, 0));
        var state = new BattleState(new BattleGrid(2, 2), [unit]);

        Assert.Same(BattleDamageRuleSettings.Neutral, state.DamageRules);
    }

    [Fact]
    public void DamageRules_NormalDifficultyDoublesPlayerTeamAttack()
    {
        var (source, target, skill) = CreateDamageScenario(sourceTeam: 1, targetTeam: 2);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var neutral = calculator.CreateSkillDamageContext(new BattleDamageContext(source, target, skill));
        var normal = calculator.CreateSkillDamageContext(new BattleDamageContext(
            source,
            target,
            skill,
            new BattleDamageRuleSettings
            {
                Difficulty = GameDifficulty.Normal,
                PlayerTeam = 1,
                EnableDifficultyDamageScaling = true,
            }));

        Assert.Equal(neutral.AttackLow * 2d, normal.AttackLow, precision: 6);
        Assert.Equal(neutral.AttackHigh * 2d, normal.AttackHigh, precision: 6);
    }

    [Fact]
    public void DamageRules_NormalDifficultyHalvesNonPlayerTeamAttack()
    {
        var (source, target, skill) = CreateDamageScenario(sourceTeam: 2, targetTeam: 1);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var neutral = calculator.CreateSkillDamageContext(new BattleDamageContext(source, target, skill));
        var normal = calculator.CreateSkillDamageContext(new BattleDamageContext(
            source,
            target,
            skill,
            new BattleDamageRuleSettings
            {
                Difficulty = GameDifficulty.Normal,
                PlayerTeam = 1,
                EnableDifficultyDamageScaling = true,
            }));

        Assert.Equal(neutral.AttackLow * 0.5d, normal.AttackLow, precision: 6);
        Assert.Equal(neutral.AttackHigh * 0.5d, normal.AttackHigh, precision: 6);
    }

    [Fact]
    public void DamageRules_RoundScalingIncreasesNonPlayerAttack()
    {
        var (source, target, skill) = CreateDamageScenario(sourceTeam: 2, targetTeam: 1);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var neutral = calculator.CreateSkillDamageContext(new BattleDamageContext(source, target, skill));
        var scaled = calculator.CreateSkillDamageContext(new BattleDamageContext(
            source,
            target,
            skill,
            new BattleDamageRuleSettings
            {
                Difficulty = GameDifficulty.Hard,
                Round = 3,
                PlayerTeam = 1,
                RoundEnemyAttackAddRatio = 0.1d,
                EnableRoundEnemyAttackDefenceScaling = true,
            }));

        Assert.Equal(neutral.AttackLow * 1.2d, scaled.AttackLow, precision: 6);
        Assert.Equal(neutral.AttackHigh * 1.2d, scaled.AttackHigh, precision: 6);
    }

    [Fact]
    public void DamageRules_RoundScalingIncreasesNonPlayerDefenceOnly()
    {
        var (source, enemyTarget, skill) = CreateDamageScenario(sourceTeam: 1, targetTeam: 2);
        var playerTarget = CreateUnit(
            "player_target",
            team: 1,
            new GridPosition(2, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Dingli] = 80,
                [StatType.Gengu] = 90,
            });
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var settings = new BattleDamageRuleSettings
        {
            Difficulty = GameDifficulty.Hard,
            Round = 3,
            PlayerTeam = 1,
            RoundEnemyDefenceAddRatio = 0.08d,
            EnableRoundEnemyAttackDefenceScaling = true,
        };

        var neutralEnemyTarget = calculator.CreateSkillDamageContext(new BattleDamageContext(source, enemyTarget, skill));
        var scaledEnemyTarget = calculator.CreateSkillDamageContext(new BattleDamageContext(source, enemyTarget, skill, settings));
        var neutralPlayerTarget = calculator.CreateSkillDamageContext(new BattleDamageContext(source, playerTarget, skill));
        var scaledPlayerTarget = calculator.CreateSkillDamageContext(new BattleDamageContext(source, playerTarget, skill, settings));

        Assert.Equal(neutralEnemyTarget.Defence * 1.16d, scaledEnemyTarget.Defence, precision: 6);
        Assert.Equal(neutralPlayerTarget.Defence, scaledPlayerTarget.Defence, precision: 6);
    }

    [Fact]
    public void DamageRules_DisabledRoundScalingLeavesAttackAndDefenceNeutral()
    {
        var (source, target, skill) = CreateDamageScenario(sourceTeam: 2, targetTeam: 2);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var neutral = calculator.CreateSkillDamageContext(new BattleDamageContext(source, target, skill));
        var disabled = calculator.CreateSkillDamageContext(new BattleDamageContext(
            source,
            target,
            skill,
            new BattleDamageRuleSettings
            {
                Difficulty = GameDifficulty.Hard,
                Round = 5,
                PlayerTeam = 1,
                RoundEnemyAttackAddRatio = 0.1d,
                RoundEnemyDefenceAddRatio = 0.08d,
                EnableRoundEnemyAttackDefenceScaling = false,
            }));

        Assert.Equal(neutral.AttackLow, disabled.AttackLow, precision: 6);
        Assert.Equal(neutral.AttackHigh, disabled.AttackHigh, precision: 6);
        Assert.Equal(neutral.Defence, disabled.Defence, precision: 6);
    }

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
        return new BattleUnit(id, character, team, position);
    }

    private static (BattleUnit Source, BattleUnit Target, SkillInstance Skill) CreateDamageScenario(
        int sourceTeam,
        int targetTeam)
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill("strike", powerBase: 10);
        var source = CreateUnit(
            "source",
            sourceTeam,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit(
            "target",
            targetTeam,
            new GridPosition(1, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Dingli] = 80,
                [StatType.Gengu] = 90,
            });

        return (source, target, source.Character.GetExternalSkills().Single());
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
