using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Tests;

public sealed class BattleRuleTests
{
    [Fact]
    public void BattleState_DefaultsToNeutralBattleRules()
    {
        var unit = CreateUnit("hero", team: 1, new GridPosition(0, 0));
        var state = new BattleState(new BattleGrid(2, 2), [unit]);

        Assert.Same(BattleRuleSettings.Neutral, state.RuleSettings);
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
            new BattleRuleSettings
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
            new BattleRuleSettings
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
            new BattleRuleSettings
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
        var settings = new BattleRuleSettings
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
            new BattleRuleSettings
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
        Assert.Empty(result.Value!.AffectedUnitIds);
        Assert.Contains(result.Messages.OfType<BattleFact>(), battleEvent => battleEvent is BattleFact { Kind: BattleFactKind.SkillCast });
        Assert.DoesNotContain(result.Messages.OfType<BattleFact>(), battleEvent =>
            battleEvent.Kind == BattleFactKind.Damaged &&
            battleEvent.UnitId == hero.Id);
    }

    [Theory]
    [InlineData(SkillImpactType.Single, false)]
    [InlineData(SkillImpactType.Plus, true)]
    [InlineData(SkillImpactType.Star, true)]
    [InlineData(SkillImpactType.Line, false)]
    [InlineData(SkillImpactType.Square, true)]
    [InlineData(SkillImpactType.Fan, false)]
    [InlineData(SkillImpactType.Ring, true)]
    [InlineData(SkillImpactType.X, true)]
    [InlineData(SkillImpactType.Cleave, false)]
    public void SkillTargetingDefaults_ResolveSelfRules(
        SkillImpactType impactType,
        bool expectedCanCastAtSelf)
    {
        Assert.Equal(expectedCanCastAtSelf, SkillTargetingDefaults.CanCastAtSelf(impactType));
        Assert.False(SkillTargetingDefaults.CanImpactSelf);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnumerateCastTargets_IncludesSourceOnlyWhenAllowed(bool canCastAtSelf)
    {
        var source = new GridPosition(1, 1);

        var targets = BattleSkillTargeting.EnumerateCastTargets(
            source,
            castSize: 1,
            canCastAtSelf,
            new BattleGrid(3, 3));

        Assert.Equal(canCastAtSelf, targets.Contains(source));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResolveEffectiveTargets_IncludesSourceOnlyWhenAllowed(bool canImpactSelf)
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "self_impact",
            impactType: SkillImpactType.Plus,
            canImpactSelf: canImpactSelf);
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(1, 1),
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var state = new BattleState(
            new BattleGrid(3, 3),
            [source],
            new BattleRuleSettings { Difficulty = GameDifficulty.Hard });

        var targets = BattleSkillTargeting.ResolveEffectiveTargets(
            state,
            source,
            source.Character.GetExternalSkills().Single(),
            new HashSet<GridPosition> { source.Position });

        Assert.Equal(canImpactSelf, targets.Contains(source));
    }

    [Fact]
    public void CastSkill_NormalDifficultyExcludesFriendlyUnits()
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
        var allySource = CreateUnit(
            "source_ally",
            team: 1,
            new GridPosition(0, 0),
            stats: stats,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var ally = CreateUnit("ally", team: 1, new GridPosition(1, 0), maxHp: 500);
        allySource.ActionGauge = 100;
        var allyState = new BattleState(new BattleGrid(4, 4), [allySource, ally]);
        var allyEngine = new BattleEngine();
        allyEngine.BeginAction(allyState, allySource.Id);
        var result = allyEngine.CastSkill(allyState, allySource.Id, allySource.Character.GetExternalSkills().Single(), ally.Position);
        var allyDamage = 500 - ally.Hp;

        Assert.True(result.Success);
        Assert.Equal(0, allyDamage);
        Assert.DoesNotContain(ally.Id, result.Value!.AffectedUnitIds);
    }

    [Fact]
    public void SkillTargetingModifierAffix_ModifiesOnlyTheConfiguredSourceSkill()
    {
        var modifiedSkillDefinition = TestContentFactory.CreateExternalSkill("modified", castSize: 2);
        var otherSkillDefinition = TestContentFactory.CreateExternalSkill("other", castSize: 2);
        var talent = new TalentDefinition
        {
            Id = "range_talent",
            Name = "range_talent",
            Affixes =
            [
                new SkillTargetingModifierAffix(
                    modifiedSkillDefinition.Id,
                    SkillTargetingField.CastSize,
                    ModifierValue.Add(3)),
            ],
        };
        var unit = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            externalSkills:
            [
                new InitialExternalSkillEntryDefinition(modifiedSkillDefinition, 1),
                new InitialExternalSkillEntryDefinition(otherSkillDefinition, 1),
            ],
            talents: [talent]);

        Assert.Equal(5, BattleSkillTargeting.ResolveEffectiveCastSize(unit, unit.Character.ExternalSkills[0]));
        Assert.Equal(2, BattleSkillTargeting.ResolveEffectiveCastSize(unit, unit.Character.ExternalSkills[1]));
    }

    [Fact]
    public void SkillTargetingModifierAffix_AppliesToDerivedFormSkill()
    {
        var form = new FormSkillDefinition(
            "source.form",
            "source.form",
            "",
            null,
            1,
            0,
            new SkillCostDefinition(),
            new SkillTargetingDefinition(CastSize: 4),
            0,
            "",
            "",
            []);
        var skillDefinition = TestContentFactory.CreateExternalSkill("source", castSize: 2, formSkills: [form]);
        var talent = new TalentDefinition
        {
            Id = "range_override_talent",
            Name = "range_override_talent",
            Affixes =
            [
                new SkillTargetingModifierAffix(
                    skillDefinition.Id,
                    SkillTargetingField.CastSize,
                    ModifierValue.Override(10)),
            ],
        };
        var unit = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)],
            talents: [talent]);
        var formSkill = unit.Character.ExternalSkills.Single().GetFormSkills().Single();

        Assert.Equal(skillDefinition.Id, formSkill.SourceSkillId);
        Assert.Equal(10, BattleSkillTargeting.ResolveEffectiveCastSize(unit, formSkill));
    }

    [Fact]
    public void GlobalSkillTargetingModifierAffix_AppliesBeforeBlindPenalty()
    {
        var firstSkill = TestContentFactory.CreateExternalSkill("first", impactSize: 1);
        var secondSkill = TestContentFactory.CreateExternalSkill("second", impactSize: 3);
        var talent = new TalentDefinition
        {
            Id = "global_impact_talent",
            Name = "global_impact_talent",
            Affixes =
            [
                new SkillTargetingModifierAffix(
                    null,
                    SkillTargetingField.ImpactSize,
                    ModifierValue.Add(1)),
            ],
        };
        var unit = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            externalSkills:
            [
                new InitialExternalSkillEntryDefinition(firstSkill, 1),
                new InitialExternalSkillEntryDefinition(secondSkill, 1),
            ],
            talents: [talent]);

        Assert.Equal(2, BattleSkillTargeting.ResolveEffectiveImpactSize(unit, unit.Character.ExternalSkills[0]));
        Assert.Equal(4, BattleSkillTargeting.ResolveEffectiveImpactSize(unit, unit.Character.ExternalSkills[1]));

        var blind = new BuffDefinition
        {
            Id = "致盲",
            Name = "致盲",
            IsDebuff = true,
        };
        unit.TryApplyBuff(new BattleBuffInstance(blind, level: 1, remainingTurns: 1, unit.Id, 0));

        Assert.Equal(1, BattleSkillTargeting.ResolveEffectiveImpactSize(unit, unit.Character.ExternalSkills[0]));
        Assert.Equal(3, BattleSkillTargeting.ResolveEffectiveImpactSize(unit, unit.Character.ExternalSkills[1]));
    }

    private static BattleUnit CreateUnit(
        string id,
        int team,
        GridPosition position,
        int maxHp = 100,
        int maxMp = 30,
        IReadOnlyDictionary<StatType, int>? stats = null,
        IReadOnlyList<InitialExternalSkillEntryDefinition>? externalSkills = null,
        IReadOnlyList<TalentDefinition>? talents = null)
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

        var definition = TestContentFactory.CreateCharacterDefinition(
            id,
            stats: mergedStats,
            externalSkills: externalSkills,
            talents: talents);
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
