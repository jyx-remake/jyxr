using System.Text.Json;
using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Tests;

public sealed class TalentAdaptationTests
{
    [Fact]
    public void CastSkill_QifengTuduAppliesToxicBuffAndAmplifiesPoisonedTarget()
    {
        var poison = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var toxic = new BuffDefinition { Id = "剧毒", Name = "剧毒", IsDebuff = true };
        var skill = TestContentFactory.CreateExternalSkill(
            "追心箭",
            powerBase: 10,
            type: WeaponType.Qimen,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        using var parameters = JsonDocument.Parse(
            """{"buffId":"中毒","maximumBonus":0.2,"referenceLevel":1}""");
        var damageEffect = new CustomBattleEffectDefinition(
            "poisoned_target_level_damage",
            parameters.RootElement.Clone());
        var qifeng = new TalentDefinition
        {
            Id = "器锋荼毒",
            Name = "器锋荼毒",
            Affixes =
            [
                new HookAffix
                {
                    Timing = HookTiming.BeforeDamageCalculation,
                    Conditions =
                    [
                        new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Source),
                        new ContextUnitRelationBattleHookConditionDefinition(
                            BattleHookContextUnitRole.Target,
                            BattleHookRelation.Enemy),
                        new ContextSkillNameEqualsBattleHookConditionDefinition(["追心箭"]),
                        new ContextUnitBuffBattleHookConditionDefinition(
                            "中毒",
                            BattleHookContextUnitRole.Target),
                    ],
                    Effects = [damageEffect],
                },
                new HookAffix
                {
                    Timing = HookTiming.OnHitConfirmed,
                    Conditions =
                    [
                        new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Source),
                        new ContextUnitRelationBattleHookConditionDefinition(
                            BattleHookContextUnitRole.Target,
                            BattleHookRelation.Enemy),
                        new ContextSkillNameEqualsBattleHookConditionDefinition(["追心箭"]),
                    ],
                    Effects =
                    [
                        new ApplyBuffBattleEffectDefinition(
                            new TargetBattleUnitSelectorDefinition(),
                            "剧毒",
                            Level: 1,
                            Duration: 1),
                    ],
                },
            ],
        };
        var repository = TestContentFactory.CreateRepository(
            talents: [qifeng],
            buffs: [poison, toxic]);
        damageEffect.Resolve(repository);
        var applyToxic = (ApplyBuffBattleEffectDefinition)((HookAffix)qifeng.Affixes[1]).Effects[0];
        applyToxic.Resolve(repository);

        var source = TestContentFactory.CreateCharacterInstance(
            "source",
            TestContentFactory.CreateCharacterDefinition(
                "source",
                stats: new Dictionary<StatType, int>
                {
                    [StatType.Qimen] = 100,
                    [StatType.Bili] = 120,
                },
                externalSkills: [new InitialExternalSkillEntryDefinition(skill, 1)],
                talents: [qifeng]));
        var target = TestContentFactory.CreateCharacterInstance(
            "target",
            TestContentFactory.CreateCharacterDefinition("target"));
        var sourceUnit = new BattleUnit("source", source, 1, new GridPosition(0, 0));
        var targetUnit = CreateUnit("target", 2, new GridPosition(1, 0), maxHp: 500);
        targetUnit.Character.RebuildSnapshot();
        targetUnit.TryApplyBuff(new BattleBuffInstance(poison, 1, 3, sourceUnit.Id, 1));
        sourceUnit.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [sourceUnit, targetUnit]);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var expectedContext = calculator.CreateSkillDamageContext(
            new BattleDamageContext(sourceUnit, targetUnit, sourceUnit.Character.GetExternalSkills().Single()));
        expectedContext.AddModifier(BattleDamageContextField.SourceAttack, ModifierOp.More, 1.2d);
        var expectedDamage = calculator.CalculateSkillDamage(expectedContext).Amount;
        var engine = new BattleEngine(calculator, random: new FixedRandomService(0.5d));

        engine.BeginAction(state, sourceUnit.Id);
        var result = engine.CastSkill(
            state,
            sourceUnit.Id,
            sourceUnit.Character.GetExternalSkills().Single(),
            targetUnit.Position);

        Assert.True(result.Success);
        Assert.Equal(500 - expectedDamage, targetUnit.Hp);
        Assert.NotNull(targetUnit.TryGetBuff("剧毒"));
    }

    [Fact]
    public void CastSkill_LiuHeFormationDoublesDefenceOnlyWithSixLivingMembers()
    {
        var skill = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        using var parameters = JsonDocument.Parse(
            """{"minimumTeamSize":6,"minimumTalentMembers":2,"chance":0.5,"defenseMultiplier":2,"talentId":"六合阵"}""");
        var defenseEffect = new CustomBattleEffectDefinition(
            "team_count_defense",
            parameters.RootElement.Clone());
        var formation = new TalentDefinition
        {
            Id = "六合阵",
            Name = "六合阵",
            Affixes =
            [
                new HookAffix
                {
                    Timing = HookTiming.BeforeDamageCalculation,
                    Conditions =
                    [new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Target)],
                    Effects = [defenseEffect],
                },
            ],
        };
        var repository = TestContentFactory.CreateRepository(talents: [formation]);
        defenseEffect.Resolve(repository);

        var sourceDefinition = TestContentFactory.CreateCharacterDefinition(
            "source",
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skill, 1)]);
        var source = new BattleUnit(
            "source",
            TestContentFactory.CreateCharacterInstance("source", sourceDefinition),
            2,
            new GridPosition(0, 0));
        var target = CreateUnit(
            "target",
            team: 1,
            new GridPosition(1, 0),
            maxHp: 5000,
            stats: new Dictionary<StatType, int>
            {
                [StatType.Dingli] = 100,
                [StatType.Gengu] = 100,
            },
            talents: [formation],
            level: 1);
        var formationAlly = CreateUnit(
            "formation_ally",
            team: 1,
            new GridPosition(2, 0),
            talents: []);
        var allies = Enumerable.Range(0, 4)
            .Select(index => CreateUnit(
                $"ally_{index}",
                team: 1,
                new GridPosition(index + 3, 0)))
            .ToList();
        target.ActionGauge = 0;
        source.ActionGauge = 100;
        var state = new BattleState(
            new BattleGrid(8, 4),
            [source, target, formationAlly, .. allies]);
        Assert.True(target.Character.HasEffectiveTalent("六合阵"));
        Assert.False(formationAlly.Character.HasEffectiveTalent("六合阵"));
        Assert.Contains(
            target.Character.GetHooks(HookTiming.BeforeDamageCalculation),
            entry => entry.Hook.Effects.Contains(defenseEffect));
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.25d));
        var expectedContext = calculator.CreateSkillDamageContext(
            new BattleDamageContext(source, target, source.Character.GetExternalSkills().Single()));
        expectedContext.AddModifier(BattleDamageContextField.TargetDefence, ModifierOp.More, 2d);
        var expectedDamage = calculator.CalculateSkillDamage(expectedContext).Amount;
        var engine = new BattleEngine(calculator, random: new FixedRandomService(0.25d));

        engine.BeginAction(state, source.Id);
        var result = engine.CastSkill(
            state,
            source.Id,
            source.Character.GetExternalSkills().Single(),
            target.Position);

        Assert.True(result.Success);
        Assert.Equal(5000 - expectedDamage, target.Hp);
    }

    private sealed class FixedRandomService(double value) : IRandomService
    {
        public double NextDouble() => value;

        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }

    private static BattleUnit CreateUnit(
        string id,
        int team,
        GridPosition position,
        int maxHp = 100,
        int maxMp = 30,
        IReadOnlyDictionary<StatType, int>? stats = null,
        IReadOnlyList<TalentDefinition>? talents = null,
        IReadOnlyList<InitialExternalSkillEntryDefinition>? externalSkills = null,
        int level = 1)
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
            talents: talents,
            externalSkills: externalSkills,
            level: level);
        return new BattleUnit(
            id,
            TestContentFactory.CreateCharacterInstance(id, definition),
            team,
            position);
    }
}
