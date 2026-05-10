using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Tests;

public sealed class BattleEngineTests
{
    [Fact]
    public void GetImpactPositions_ReturnsLegacyFaceSquare()
    {
        var target = new GridPosition(3, 3);

        var positions = BattleEngine.GetImpactPositions(
            new GridPosition(0, 0),
            target,
            SkillImpactType.Square,
            impactSize: 3);

        var expected = new HashSet<GridPosition>
        {
            new(2, 2),
            new(3, 2),
            new(4, 2),
            new(2, 3),
            new(3, 3),
            new(4, 3),
            new(2, 4),
            new(3, 4),
            new(4, 4),
        };

        Assert.Equal(expected.Count, positions.Count);
        foreach (var position in expected)
        {
            Assert.Contains(position, positions);
        }

        Assert.DoesNotContain(new GridPosition(1, 3), positions);
        Assert.DoesNotContain(new GridPosition(5, 3), positions);
    }

    [Fact]
    public void GetImpactPositions_ReturnsEightDirectionStar()
    {
        var target = new GridPosition(3, 3);

        var positions = BattleEngine.GetImpactPositions(
            new GridPosition(0, 0),
            target,
            SkillImpactType.Star,
            impactSize: 2);

        var expected = new HashSet<GridPosition>
        {
            new(3, 3),
            new(3, 1),
            new(3, 2),
            new(3, 4),
            new(3, 5),
            new(1, 3),
            new(2, 3),
            new(4, 3),
            new(5, 3),
            new(1, 1),
            new(2, 2),
            new(4, 4),
            new(5, 5),
            new(1, 5),
            new(2, 4),
            new(4, 2),
            new(5, 1),
        };

        Assert.Equal(expected.Count, positions.Count);
        foreach (var position in expected)
        {
            Assert.Contains(position, positions);
        }

        Assert.DoesNotContain(new GridPosition(2, 1), positions);
        Assert.DoesNotContain(new GridPosition(4, 5), positions);
    }

    [Fact]
    public void AdvanceUntilNextAction_SelectsHighestReadyGauge()
    {
        var slow = CreateUnit("slow", team: 1, new GridPosition(0, 0), actionSpeed: 10);
        var fast = CreateUnit("fast", team: 1, new GridPosition(1, 0), actionSpeed: 30);
        var state = new BattleState(new BattleGrid(4, 4), [slow, fast]);
        var engine = new BattleEngine();

        var acting = engine.AdvanceUntilNextAction(state);

        Assert.Equal("fast", acting.Id);
        Assert.NotNull(state.CurrentAction);
        Assert.Equal("fast", state.CurrentAction.ActingUnitId);
    }

    [Fact]
    public void Rest_UsesLegacyBaseFormula()
    {
        var internalSkill = TestContentFactory.CreateInternalSkill("inner", yin: 150);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            maxHp: 1000,
            maxMp: 500,
            hp: 400,
            mp: 100,
            stats: new Dictionary<StatType, int>
            {
                [StatType.Gengu] = 100,
            },
            internalSkills: [new InitialInternalSkillEntryDefinition(internalSkill, Level: 10, Equipped: true)]);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var engine = new BattleEngine(random: new FixedRandomService(0.5d));
        engine.BeginAction(state, hero.Id);

        var result = engine.Rest(state, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(575, hero.Hp);
        Assert.Equal(430, hero.Mp);
        Assert.Equal(string.Empty, result.Message);
        var restEvent = Assert.Single(result.Events.Where(static battleEvent => battleEvent.Kind == BattleEventKind.Rested));
        Assert.Equal(new BattleRestRecovery(175, 330), restEvent.Rest);
        Assert.Null(state.CurrentAction);
    }

    [Fact]
    public void MoveTo_ChargesExtraCost_WhenEnteringEnemyZone()
    {
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 1), movePower: 3);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(2, 1));
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(5, 5), [hero, enemy]);
        var engine = new BattleEngine();
        engine.BeginAction(state, hero.Id);

        var result = engine.MoveTo(state, hero.Id, new GridPosition(1, 1));

        Assert.True(result.Success);
        Assert.Equal(new GridPosition(1, 1), hero.Position);
        Assert.Equal(1, state.CurrentAction!.RemainingMovePower);
    }

    [Fact]
    public void MoveTo_DoesNotChargeZoneCost_WhenTraitIgnoresZoneOfControl()
    {
        var talent = new TalentDefinition
        {
            Id = "ignore_zoc",
            Name = "ignore_zoc",
            Affixes = [new TraitAffix(TraitId.IgnoreZoneOfControl)],
        };
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 1), movePower: 3, talents: [talent]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(2, 1));
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(5, 5), [hero, enemy]);
        var engine = new BattleEngine();
        engine.BeginAction(state, hero.Id);

        var result = engine.MoveTo(state, hero.Id, new GridPosition(1, 1));

        Assert.True(result.Success);
        Assert.Equal(2, state.CurrentAction!.RemainingMovePower);
    }

    [Fact]
    public void HasTrait_ReturnsTrue_WhenActiveBuffProvidesTrait()
    {
        var buff = new BuffDefinition
        {
            Id = "ghost_step",
            Name = "ghost_step",
            IsDebuff = false,
            Affixes = [new TraitAffix(TraitId.IgnoreZoneOfControl)],
        };
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0));

        hero.ApplyBuff(new BattleBuffInstance(buff, level: 1, remainingTurns: 2, sourceUnitId: hero.Id, appliedAtActionSerial: 0));

        Assert.True(hero.HasTrait(TraitId.IgnoreZoneOfControl));
    }

    [Fact]
    public void RollbackMove_RestoresStartPositionAndMovePower()
    {
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0), movePower: 3);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var engine = new BattleEngine();
        engine.BeginAction(state, hero.Id);
        engine.MoveTo(state, hero.Id, new GridPosition(1, 0));

        var result = engine.RollbackMove(state, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(new GridPosition(0, 0), hero.Position);
        Assert.Equal(3, state.CurrentAction!.RemainingMovePower);
        Assert.False(state.CurrentAction.HasMoved);
    }

    [Fact]
    public void CastSkill_UsesExistingSkillInstance_ConsumesResourcesAndAppliesBuff()
    {
        var buff = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            mpCost: 5,
            rageCost: 2,
            cooldown: 1,
            powerBase: 10,
            buffs: [new SkillBuffDefinition(buff, level: 2, duration: 3)],
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            maxMp: 20,
            rage: 10,
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(2, 0), maxHp: 500);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(5, 5), [hero, enemy]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(1d));
        engine.BeginAction(state, hero.Id);

        var skill = hero.Character.GetExternalSkills().Single();
        var result = engine.CastSkill(state, hero.Id, skill, enemy.Position);

        Assert.True(result.Success);
        Assert.Null(state.CurrentAction);
        Assert.Equal(15, hero.Mp);
        Assert.Equal(4, hero.Rage);
        Assert.True(enemy.Hp < 500);
        Assert.Equal(1, skill.CurrentCooldown);
        Assert.Contains(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.Damaged &&
            battleEvent.UnitId == enemy.Id);
        var appliedBuff = Assert.Single(enemy.Buffs);
        Assert.Equal("中毒", appliedBuff.Definition.Id);
        Assert.Equal(2, appliedBuff.Level);
        Assert.Equal(3, appliedBuff.RemainingTurns);
    }

    [Fact]
    public void CastSkill_DoesNotApplyBuff_WhenChanceRollFails()
    {
        var buff = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            buffs: [new SkillBuffDefinition(buff, level: 1, duration: 2, chance: 50)],
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0));
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(random: new FixedRandomService(0.75d));
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), enemy.Position);

        Assert.True(result.Success);
        Assert.Empty(enemy.Buffs);
        Assert.DoesNotContain(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.BuffApplied &&
            battleEvent.UnitId == enemy.Id &&
            battleEvent.Detail == "中毒");
    }

    [Fact]
    public void CastSkill_AppliesBuff_WhenChanceRollSucceeds()
    {
        var buff = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            buffs: [new SkillBuffDefinition(buff, level: 1, duration: 2, chance: 50)],
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0));
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(random: new FixedRandomService(0.25d));
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), enemy.Position);

        Assert.True(result.Success);
        var appliedBuff = Assert.Single(enemy.Buffs);
        Assert.Equal("中毒", appliedBuff.Definition.Id);
        Assert.Contains(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.BuffApplied &&
            battleEvent.UnitId == enemy.Id &&
            battleEvent.Detail == "中毒");
    }

    [Fact]
    public void CastSkill_RecordsCriticalDamageEvent()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
                [StatType.CritChance] = 1,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0), maxHp: 500);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(new BattleDamageCalculator(new FixedRandomService(0.5d)));
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), enemy.Position);

        Assert.True(result.Success);
        var damageEvent = Assert.Single(state.Events.Where(battleEvent =>
            battleEvent.Kind == BattleEventKind.Damaged &&
            battleEvent.UnitId == enemy.Id));
        Assert.NotNull(damageEvent.Damage);
        Assert.True(damageEvent.Damage.IsCritical);
        Assert.Equal(hero.Id, damageEvent.Damage.SourceUnitId);
        Assert.True(damageEvent.Damage.Amount > 0);
    }

    [Fact]
    public void CastSkill_RequiresBaseRageBeforeLegendResolution()
    {
        var stagger = new BuffDefinition { Id = "stagger", Name = "stagger", IsDebuff = true };
        var heroic = new TalentDefinition { Id = "heroic", Name = "heroic" };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            mpCost: 3,
            rageCost: 4,
            cooldown: 2,
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3,
            audio: "base_audio",
            animation: "base_anim");
        var legendSkill = new LegendSkillDefinition(
            Id: "strike_legend",
            Name: "奥义.天外流星",
            StartSkill: "strike",
            Probability: 1d,
            Conditions: [new RequiredTalentLegendConditionDefinition("heroic")],
            Buffs: [new SkillBuffDefinition(stagger, level: 2, duration: 2)],
            PowerExtra: 12d,
            RequiredLevel: 1,
            Animation: "aoyi_fx");
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            maxMp: 20,
            mp: 10,
            rage: 0,
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
                [StatType.Wuxing] = 100,
            },
            talents: [heroic],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0), maxHp: 500);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(1d),
            legendSkillsProvider: () => [legendSkill]);
        engine.BeginAction(state, hero.Id);

        var skill = hero.Character.GetExternalSkills().Single();
        var result = engine.CastSkill(state, hero.Id, skill, enemy.Position);

        Assert.False(result.Success);
        Assert.Equal("Not enough rage.", result.Message);
        Assert.Equal(0, hero.Rage);
        Assert.Equal(10, hero.Mp);
        Assert.Equal(0, skill.CurrentCooldown);
        Assert.DoesNotContain(state.Events, battleEvent => battleEvent.Kind == BattleEventKind.SkillCast);
        Assert.Empty(enemy.Buffs);
    }

    [Fact]
    public void CastSkill_UsesBaseRageValidationWhenLegendConditionsAreNotMet()
    {
        var heroic = new TalentDefinition { Id = "heroic", Name = "heroic" };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            mpCost: 3,
            rageCost: 4,
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var legendSkill = new LegendSkillDefinition(
            Id: "strike_legend",
            Name: "奥义.天外流星",
            StartSkill: "strike",
            Probability: 1d,
            Conditions: [new RequiredTalentLegendConditionDefinition("heroic")],
            Buffs: [],
            PowerExtra: 12d,
            RequiredLevel: 1,
            Animation: "aoyi_fx");
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            maxMp: 20,
            mp: 10,
            rage: 0,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0), maxHp: 500);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(
            random: new FixedRandomService(1d),
            legendSkillsProvider: () => [legendSkill]);
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), enemy.Position);

        Assert.False(result.Success);
        Assert.Equal("Not enough rage.", result.Message);
    }

    [Fact]
    public void CastSkill_AppliesLegendChanceBonusAsProbabilityMultiplier()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var legendSkill = new LegendSkillDefinition(
            Id: "strike_legend",
            Name: "奥义.天外流星",
            StartSkill: "strike",
            Probability: 0.2d,
            Conditions: [],
            Buffs: [],
            PowerExtra: 12d,
            RequiredLevel: 1,
            Animation: "aoyi_fx");
        var chanceAffix = new SkillAffixDefinition(
            new LegendSkillChanceModifierAffix("strike_legend", ModifierValue.Add(0.1d)));
        var chanceSkillDefinition = TestContentFactory.CreateExternalSkill(
            "chance_source",
            affixes: [chanceAffix]);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills:
            [
                new InitialExternalSkillEntryDefinition(skillDefinition, 1),
                new InitialExternalSkillEntryDefinition(chanceSkillDefinition, 1),
            ]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0), maxHp: 500);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.25d),
            legendSkillsProvider: () => [legendSkill]);
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(
            state,
            hero.Id,
            hero.Character.GetExternalSkills().Single(skill => skill.Id == "strike"),
            enemy.Position);

        Assert.True(result.Success);
        Assert.False(result.SkillCast?.IsLegend);
    }

    [Fact]
    public void CastSkill_BroadLearningTraitAddsLegendChanceMultiplier()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var legendSkill = new LegendSkillDefinition(
            Id: "strike_legend",
            Name: "奥义.天外流星",
            StartSkill: "strike",
            Probability: 0.2d,
            Conditions: [],
            Buffs: [],
            PowerExtra: 12d,
            RequiredLevel: 1,
            Animation: "aoyi_fx");
        var broadLearning = new TalentDefinition
        {
            Id = "broad_learning",
            Name = "博览群书",
            Affixes = [new TraitAffix(TraitId.BroadLearning)],
        };
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            talents: [broadLearning],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0), maxHp: 500);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.25d),
            legendSkillsProvider: () => [legendSkill]);
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(
            state,
            hero.Id,
            hero.Character.GetExternalSkills().Single(),
            enemy.Position);

        Assert.True(result.Success);
        Assert.True(result.SkillCast?.IsLegend);
    }

    [Fact]
    public void CastSkill_LegendAreaSkillDoesNotDamageAllies()
    {
        var heroic = new TalentDefinition { Id = "heroic", Name = "heroic" };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            rageCost: 2,
            powerBase: 10,
            impactType: SkillImpactType.Square,
            impactSize: 1,
            castSize: 3);
        var legendSkill = new LegendSkillDefinition(
            Id: "strike_legend",
            Name: "奥义.天外流星",
            StartSkill: "strike",
            Probability: 1d,
            Conditions: [new RequiredTalentLegendConditionDefinition("heroic")],
            Buffs: [],
            PowerExtra: 12d,
            RequiredLevel: 1,
            Animation: "aoyi_fx");
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            rage: 2,
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            talents: [heroic],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var ally = CreateUnit("ally", team: 1, new GridPosition(1, 1), maxHp: 500);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0), maxHp: 500);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(5, 5), [hero, ally, enemy]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(1d),
            legendSkillsProvider: () => [legendSkill]);
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), enemy.Position);

        Assert.True(result.Success);
        Assert.True(result.SkillCast?.IsLegend);
        Assert.Equal(0, hero.Rage);
        Assert.Equal(500, ally.Hp);
        Assert.True(enemy.Hp < 500);
        Assert.DoesNotContain(ally.Id, result.AffectedUnitIds);
        Assert.Contains(enemy.Id, result.AffectedUnitIds);
    }

    [Fact]
    public void ActiveBuffs_ReturnOnlyHighestLevelPerBuffDefinition()
    {
        var buff = new BuffDefinition { Id = "shield", Name = "shield", IsDebuff = false };
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0));
        hero.ApplyBuff(new BattleBuffInstance(buff, level: 1, remainingTurns: 5, "hero", 1));
        hero.ApplyBuff(new BattleBuffInstance(buff, level: 3, remainingTurns: 1, "hero", 2));

        var active = hero.GetActiveBuffs();

        var activeBuff = Assert.Single(active);
        Assert.Equal(3, activeBuff.Level);
    }

    [Fact]
    public void AddRage_ClampsToBattleRageCap()
    {
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0), rage: 5);

        hero.AddRage(10);

        Assert.Equal(BattleUnit.MaxRage, hero.Rage);
    }

    [Fact]
    public void CastSkill_CanGrantRageToAttackerAndDamagedEnemy()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 1,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0));
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(new BattleDamageCalculator(new FixedRandomService(0.5d)), random: new FixedRandomService(0.1d));
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), enemy.Position);

        Assert.True(result.Success);
        Assert.Equal(1, hero.Rage);
        Assert.Equal(1, enemy.Rage);
        Assert.Contains(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.RageChanged &&
            battleEvent.UnitId == hero.Id &&
            battleEvent.Detail == "attack:1");
        Assert.Contains(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.RageChanged &&
            battleEvent.UnitId == enemy.Id &&
            battleEvent.Detail == "damaged:1");
    }

    [Fact]
    public void AdvanceUntilNextAction_AppliesPoisonOnTimelineRound()
    {
        var poison = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0), hp: 100, stats: new Dictionary<StatType, int>
        {
            [StatType.Dingli] = 0,
        }, actionSpeed: 1);
        hero.ApplyBuff(new BattleBuffInstance(poison, level: 2, remainingTurns: 2, "enemy", 0));
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var engine = new BattleEngine(random: new FixedRandomService(0.5d));

        Assert.Throws<InvalidOperationException>(() => engine.AdvanceUntilNextAction(state, maxTicks: 50));

        Assert.Equal(48, hero.Hp);
        Assert.Equal(1, Assert.Single(hero.Buffs).RemainingTurns);
        Assert.Contains(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.Damaged &&
            battleEvent.UnitId == hero.Id &&
            battleEvent.Detail == "中毒" &&
            battleEvent.Damage is { Amount: 52, IsCritical: false });
    }

    [Fact]
    public void EndAction_PreservesActionGaugeOverflow()
    {
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0));
        var ally = CreateUnit("ally", team: 1, new GridPosition(1, 0));
        var state = new BattleState(new BattleGrid(4, 4), [hero, ally]);
        var engine = new BattleEngine();

        hero.ActionGauge = 120d;
        engine.BeginAction(state, hero.Id);

        var result = engine.EndAction(state, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(20d, hero.ActionGauge);
    }

    [Fact]
    public void DamageCalculator_UsesActiveBuffStatModifiers()
    {
        var attackUp = new BuffDefinition
        {
            Id = "attack_up",
            Name = "attack_up",
            IsDebuff = false,
            Affixes = [new StatModifierAffix(StatType.Attack, ModifierValue.Add(100))],
        };
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            });
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0));
        var skillDefinition = TestContentFactory.CreateExternalSkill("strike", powerBase: 10);
        source.Character.SetExternalSkillState(skillDefinition, level: 1, exp: 0, active: true);
        var skill = source.Character.GetExternalSkills().Single();
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var withoutBuff = calculator.CalculateSkillDamage(new BattleDamageContext(source, target, skill));
        source.ApplyBuff(new BattleBuffInstance(attackUp, level: 1, remainingTurns: 3, source.Id, 1));

        var withBuff = calculator.CalculateSkillDamage(new BattleDamageContext(source, target, skill));

        Assert.True(withBuff.Amount > withoutBuff.Amount);
    }

    [Fact]
    public void BeginAction_UsesBuffLevelMovementModifiers()
    {
        var light = new BuffDefinition
        {
            Id = "轻身",
            Name = "轻身",
            IsDebuff = false,
            Affixes = [new BuffLevelStatModifierAffix(StatType.Movement, AddBase: 1, AddPerLevel: 1)],
        };
        var slow = new BuffDefinition
        {
            Id = "缓速",
            Name = "缓速",
            IsDebuff = true,
            Affixes = [new BuffLevelStatModifierAffix(StatType.Movement, AddPerLevel: -1.5)],
        };
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            movePower: null,
            stats: new Dictionary<StatType, int>
            {
                [StatType.Shenfa] = 101,
            });
        hero.ApplyBuff(new BattleBuffInstance(light, level: 2, remainingTurns: 3, hero.Id, 1));
        hero.ApplyBuff(new BattleBuffInstance(slow, level: 1, remainingTurns: 3, hero.Id, 2));
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(8, 8), [hero]);
        var engine = new BattleEngine();

        engine.BeginAction(state, hero.Id);

        Assert.Equal(5, state.CurrentAction!.RemainingMovePower);
    }

    [Fact]
    public void CastSkill_UsesInternalInjuryCostHookForValidationAndSpending()
    {
        var internalInjury = new BuffDefinition
        {
            Id = "内伤",
            Name = "内伤",
            IsDebuff = true,
            Affixes = [CreateInternalInjuryCostHook()],
        };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            mpCost: 10,
            powerBase: 1,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            maxMp: 30,
            mp: 12,
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0));
        hero.ApplyBuff(new BattleBuffInstance(internalInjury, level: 2, remainingTurns: 3, enemy.Id, 1));
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(new BattleDamageCalculator(new FixedRandomService(0.5d)));
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), enemy.Position);

        Assert.True(result.Success);
        Assert.Equal(0, hero.Mp);
    }

    [Fact]
    public void CastSkill_FailsWhenInternalInjuryCostHookExceedsCurrentMp()
    {
        var internalInjury = new BuffDefinition
        {
            Id = "内伤",
            Name = "内伤",
            IsDebuff = true,
            Affixes = [CreateInternalInjuryCostHook()],
        };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            mpCost: 10,
            powerBase: 1,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            maxMp: 30,
            mp: 11,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0));
        hero.ApplyBuff(new BattleBuffInstance(internalInjury, level: 2, remainingTurns: 3, enemy.Id, 1));
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine();
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), enemy.Position);

        Assert.False(result.Success);
        Assert.Equal("Not enough MP.", result.Message);
        Assert.Equal(11, hero.Mp);
    }

    [Fact]
    public void EvaluateSkillAvailability_UsesResolvedMpCostAndReportsUnavailableStatus()
    {
        var internalInjury = new BuffDefinition
        {
            Id = "内伤",
            Name = "内伤",
            IsDebuff = true,
            Affixes = [CreateInternalInjuryCostHook()],
        };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            mpCost: 10,
            powerBase: 1,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            maxMp: 30,
            mp: 11,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0));
        hero.ApplyBuff(new BattleBuffInstance(internalInjury, level: 2, remainingTurns: 3, enemy.Id, 1));
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine();

        var availability = engine.EvaluateSkillAvailability(state, hero.Id, hero.Character.GetExternalSkills().Single());

        Assert.Equal(12, availability.MpCost);
        Assert.False(availability.IsAvailable);
        Assert.Equal(BattleSkillAvailabilityStatus.NotEnoughMp, availability.Status);
    }

    [Fact]
    public void EvaluateSkillAvailability_ReportsCooldownBeforeAllowingSelection()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            mpCost: 3,
            cooldown: 2,
            powerBase: 1,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var skill = hero.Character.GetExternalSkills().Single();
        skill.CurrentCooldown = 2;
        var engine = new BattleEngine();

        var availability = engine.EvaluateSkillAvailability(state, hero.Id, skill);

        Assert.False(availability.IsAvailable);
        Assert.Equal(BattleSkillAvailabilityStatus.Cooldown, availability.Status);
        Assert.Equal(2, availability.RemainingCooldown);
    }

    [Fact]
    public void EvaluateSkillAvailability_RejectsRandomBeforeSkillCostHookInPreviewMode()
    {
        var randomCostBuff = new BuffDefinition
        {
            Id = "random_cost",
            Name = "random_cost",
            IsDebuff = true,
            Affixes =
            [
                new HookAffix
                {
                    Timing = HookTiming.BeforeSkillCost,
                    Conditions =
                    [
                        new ChanceBattleHookConditionDefinition(0.5d),
                    ],
                    Effects =
                    [
                        new ModifyMpCostBattleHookEffectDefinition(ModifierOp.Increase, Delta: 0.5d),
                    ],
                },
            ],
        };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            mpCost: 10,
            powerBase: 1,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0));
        hero.ApplyBuff(new BattleBuffInstance(randomCostBuff, level: 1, remainingTurns: 3, enemy.Id, 1));
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine();

        Assert.Throws<InvalidOperationException>(() =>
            engine.EvaluateSkillAvailability(state, hero.Id, hero.Character.GetExternalSkills().Single()));
    }

    [Fact]
    public void CastSkill_PoisonMasteryStrengthensPoisonBeforeApplication()
    {
        var poison = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var talent = new TalentDefinition
        {
            Id = "毒系精通",
            Name = "毒系精通",
            Affixes = [CreatePoisonMasteryHook()],
        };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "poison_strike",
            powerBase: 1,
            buffs: [new SkillBuffDefinition(poison, level: 2, duration: 3)],
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            talents: [talent],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0));
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(new BattleDamageCalculator(new FixedRandomService(0.5d)));
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), enemy.Position);

        Assert.True(result.Success);
        var applied = Assert.Single(enemy.Buffs);
        Assert.Equal(5, applied.Level);
        Assert.Equal(5, applied.RemainingTurns);
    }

    [Fact]
    public void BeginAction_HookAppliedBuffRunsBeforeBuffAppliedLifecycle()
    {
        var recovery = new BuffDefinition { Id = "恢复", Name = "恢复", IsDebuff = false };
        var applyBuffHook = new HookAffix
        {
            Timing = HookTiming.BeforeActionStart,
            Effects =
            [
                new ApplyBuffBattleEffectDefinition(
                    new SelfBattleTargetSelectorDefinition(),
                    recovery.Id,
                    Level: 1,
                    Duration: 2),
            ],
        };
        var strengthenHook = new HookAffix
        {
            Timing = HookTiming.BeforeBuffApplied,
            Conditions = [new ContextBuffIdBattleHookConditionDefinition(recovery.Id)],
            Effects = [new StrengthenContextBuffBattleHookEffectDefinition(LevelDelta: 2, TurnDelta: 1)],
        };
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            talents:
            [
                CreateDamageContextTalent("普照", applyBuffHook),
                CreateDamageContextTalent("疗伤精通", strengthenHook),
            ]);
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var engine = new BattleEngine(buffResolver: id => id == recovery.Id ? recovery : throw new KeyNotFoundException(id));

        var action = engine.BeginAction(state, hero.Id);

        Assert.NotNull(action);
        var applied = Assert.Single(hero.Buffs);
        Assert.Equal(3, applied.Level);
        Assert.Equal(3, applied.RemainingTurns);
        Assert.Contains(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.BuffApplied &&
            battleEvent.UnitId == hero.Id &&
            battleEvent.Detail == "BeforeActionStart:恢复");
    }

    [Fact]
    public void CastSkill_HookRemovedBuffRunsOnBuffRemovedLifecycle()
    {
        var poison = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var removeBuffHook = new HookAffix
        {
            Timing = HookTiming.OnHitConfirmed,
            Effects =
            [
                new RemoveBuffBattleEffectDefinition(
                    new TargetBattleTargetSelectorDefinition(),
                    poison.Id),
            ],
        };
        var removedRageHook = new HookAffix
        {
            Timing = HookTiming.OnBuffRemoved,
            Conditions = [new ContextBuffIdBattleHookConditionDefinition(poison.Id)],
            Effects =
            [
                new AddRageBattleEffectDefinition(
                    new SelfBattleTargetSelectorDefinition(),
                    Value: 2),
            ],
        };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 1,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            talents: [CreateDamageContextTalent("净化攻击", removeBuffHook)],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit(
            "target",
            team: 2,
            new GridPosition(1, 0),
            maxHp: 500,
            talents: [CreateDamageContextTalent("去毒回怒", removedRageHook)]);
        target.ApplyBuff(new BattleBuffInstance(poison, level: 1, remainingTurns: 3, source.Id, 1));
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.1d));
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.Empty(target.Buffs);
        Assert.Equal(3, target.Rage);
        Assert.Contains(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.BuffRemoved &&
            battleEvent.UnitId == target.Id &&
            battleEvent.Detail == "OnHitConfirmed:中毒");
        Assert.Contains(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.RageChanged &&
            battleEvent.UnitId == target.Id &&
            battleEvent.Detail == "OnBuffRemoved:2");
    }

    [Fact]
    public void CastSkill_AppliesDebuffsToTargetAndBuffsToSource()
    {
        var poison = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var recovery = new BuffDefinition { Id = "恢复", Name = "恢复", IsDebuff = false };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "mixed_buff_strike",
            powerBase: 1,
            buffs:
            [
                new SkillBuffDefinition(poison, level: 2, duration: 3),
                new SkillBuffDefinition(recovery, level: 1, duration: 2),
            ],
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0));
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [hero, enemy]);
        var engine = new BattleEngine(new BattleDamageCalculator(new FixedRandomService(0.5d)));
        engine.BeginAction(state, hero.Id);

        var result = engine.CastSkill(state, hero.Id, hero.Character.GetExternalSkills().Single(), enemy.Position);

        Assert.True(result.Success);
        var sourceBuff = Assert.Single(hero.Buffs);
        Assert.Equal(recovery.Id, sourceBuff.Definition.Id);
        var targetBuff = Assert.Single(enemy.Buffs);
        Assert.Equal(poison.Id, targetBuff.Definition.Id);
    }

    [Fact]
    public void BeginAction_PuzhaoAppliesRecoveryBuffToNearbyAllies()
    {
        var talent = new TalentDefinition
        {
            Id = "普照",
            Name = "普照",
            Affixes = [CreatePuzhaoHook()],
        };
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0), talents: [talent]);
        var ally = CreateUnit("ally", team: 1, new GridPosition(2, 0));
        var distant = CreateUnit("distant", team: 1, new GridPosition(4, 0));
        var enemy = CreateUnit("enemy", team: 2, new GridPosition(1, 0));
        var recoveryBuff = new BuffDefinition
        {
            Id = "恢复",
            Name = "恢复",
            IsDebuff = false,
        };
        hero.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [hero, ally, distant, enemy]);
        var engine = new BattleEngine(
            random: new FixedRandomService(0.1d),
            buffResolver: id => id == recoveryBuff.Id
                ? recoveryBuff
                : throw new KeyNotFoundException(id));

        engine.BeginAction(state, hero.Id);

        Assert.Equal("恢复", Assert.Single(hero.Buffs).Definition.Id);
        Assert.Equal("恢复", Assert.Single(ally.Buffs).Definition.Id);
        Assert.Empty(distant.Buffs);
        Assert.Empty(enemy.Buffs);
    }

    [Fact]
    public void AdvanceUntilNextAction_ReducesCooldownEveryTimelineRound()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill("strike", cooldown: 3, powerBase: 1);
        var hero = CreateUnit(
            "hero",
            team: 1,
            new GridPosition(0, 0),
            actionSpeed: 1,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        hero.Character.GetExternalSkills().Single().CurrentCooldown = 2;
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var engine = new BattleEngine();

        Assert.Throws<InvalidOperationException>(() => engine.AdvanceUntilNextAction(state, maxTicks: 50));

        Assert.Equal(1, hero.Character.GetExternalSkills().Single().CurrentCooldown);
    }

    [Fact]
    public void CastSkill_QiankunShiftCanHalveIncomingDamage()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var sourceStats = new Dictionary<StatType, int>
        {
            [StatType.Quanzhang] = 100,
            [StatType.Bili] = 120,
        };
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: sourceStats,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var plainTarget = CreateUnit("plain", team: 2, new GridPosition(1, 0), maxHp: 500);
        var talent = new TalentDefinition
        {
            Id = "乾坤大挪移",
            Name = "乾坤大挪移",
            Affixes = [CreateQiankunShiftHook()],
        };
        var qiankunTarget = CreateUnit("qiankun", team: 2, new GridPosition(1, 1), maxHp: 500, talents: [talent]);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var expected = calculator.CalculateSkillDamage(new BattleDamageContext(source, plainTarget, source.Character.GetExternalSkills().Single())).Amount;
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, qiankunTarget]);
        var engine = new BattleEngine(calculator, random: new FixedRandomService(0.1d));
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), qiankunTarget.Position);

        Assert.True(result.Success);
        Assert.Equal(500 - expected / 2, qiankunTarget.Hp);
    }

    [Fact]
    public void CastSkill_BeforeHitResolvedCancelHit_SuppressesHitEffectsAndSkipsOnDamageTaken()
    {
        var debuff = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            buffs: [new SkillBuffDefinition(debuff, level: 1, duration: 2)],
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit(
            "target",
            team: 2,
            new GridPosition(1, 0),
            maxHp: 500,
            talents:
            [
                CreateDamageContextTalent("易容", CreateBeforeHitCancelHook()),
                CreateDamageContextTalent(
                    "受击回怒",
                    new HookAffix
                    {
                        Timing = HookTiming.OnDamageTaken,
                        Conditions = [new DamagePositiveBattleHookConditionDefinition()],
                        Effects =
                        [
                            new AddRageBattleEffectDefinition(
                                new SelfBattleTargetSelectorDefinition(),
                                Value: 2),
                        ],
                    }),
            ]);
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.1d));
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.Equal(500, target.Hp);
        Assert.Equal(0, target.Rage);
        Assert.Empty(target.Buffs);

        var damageEvent = Assert.Single(state.Events.Where(battleEvent =>
            battleEvent.Kind == BattleEventKind.Damaged &&
            battleEvent.UnitId == target.Id));
        Assert.Equal(0, damageEvent.Damage?.Amount ?? -1);
        Assert.DoesNotContain(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.RageChanged &&
            battleEvent.UnitId == target.Id &&
            string.Equals(battleEvent.Detail, "OnDamageTaken:2", StringComparison.Ordinal));
    }

    [Fact]
    public void CastSkill_BeforeHitResolvedSourceOverrideAndFumbleUseLegacyOrder()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            talents:
            [
                CreateDamageContextTalent("破闪避", CreateSetHitSuccessWhenMissHook()),
                CreateDamageContextTalent("白内障", CreateSourceFumbleHook()),
            ],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit(
            "target",
            team: 2,
            new GridPosition(1, 0),
            maxHp: 500,
            talents:
            [
                CreateDamageContextTalent("闪避", CreateBeforeHitCancelHook()),
            ]);
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.1d));
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.Equal(500, target.Hp);
        var damageEvent = Assert.Single(state.Events.Where(battleEvent =>
            battleEvent.Kind == BattleEventKind.Damaged &&
            battleEvent.UnitId == target.Id));
        Assert.Equal(0, damageEvent.Damage?.Amount ?? -1);
    }

    [Fact]
    public void CastSkill_BeforeHitResolvedSetHitSuccessRestoresHitEffects()
    {
        var debuff = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            buffs: [new SkillBuffDefinition(debuff, level: 1, duration: 2)],
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            talents:
            [
                CreateDamageContextTalent("破闪避", CreateSetHitSuccessWhenMissHook()),
            ],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit(
            "target",
            team: 2,
            new GridPosition(1, 0),
            maxHp: 500,
            talents:
            [
                CreateDamageContextTalent("闪避", CreateBeforeHitCancelHook()),
            ]);
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.1d));
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.True(target.Hp < 500);
        var buff = Assert.Single(target.Buffs);
        Assert.Equal("中毒", buff.Definition.Id);
    }

    [Fact]
    public void CastSkill_BeforeHitResolvedGenderConditionMatchesFemaleAndEunuch()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var talent = CreateDamageContextTalent("段王爷的电眼", CreateFemaleOrEunuchMissHook());
        var femaleSource = CreateUnit(
            "female_source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)],
            gender: CharacterGender.Female);
        var eunuchSource = CreateUnit(
            "eunuch_source",
            team: 1,
            new GridPosition(0, 1),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)],
            gender: CharacterGender.Eunuch);
        var maleSource = CreateUnit(
            "male_source",
            team: 1,
            new GridPosition(0, 2),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)],
            gender: CharacterGender.Male);
        var femaleTarget = CreateUnit("female_target", team: 2, new GridPosition(1, 0), maxHp: 500, talents: [talent]);
        var eunuchTarget = CreateUnit("eunuch_target", team: 2, new GridPosition(1, 1), maxHp: 500, talents: [talent]);
        var maleTarget = CreateUnit("male_target", team: 2, new GridPosition(1, 2), maxHp: 500, talents: [talent]);
        var state = new BattleState(new BattleGrid(4, 4), [femaleSource, eunuchSource, maleSource, femaleTarget, eunuchTarget, maleTarget]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.1d));

        femaleSource.ActionGauge = 100;
        engine.BeginAction(state, femaleSource.Id);
        var femaleResult = engine.CastSkill(state, femaleSource.Id, femaleSource.Character.GetExternalSkills().Single(), femaleTarget.Position);

        eunuchSource.ActionGauge = 100;
        engine.BeginAction(state, eunuchSource.Id);
        var eunuchResult = engine.CastSkill(state, eunuchSource.Id, eunuchSource.Character.GetExternalSkills().Single(), eunuchTarget.Position);

        maleSource.ActionGauge = 100;
        engine.BeginAction(state, maleSource.Id);
        var maleResult = engine.CastSkill(state, maleSource.Id, maleSource.Character.GetExternalSkills().Single(), maleTarget.Position);

        Assert.True(femaleResult.Success);
        Assert.True(eunuchResult.Success);
        Assert.True(maleResult.Success);
        Assert.Equal(500, femaleTarget.Hp);
        Assert.Equal(500, eunuchTarget.Hp);
        Assert.True(maleTarget.Hp < 500);
    }

    [Fact]
    public void CastSkill_OnHitConfirmedRunsBeforeSkillBuffsAndSkipsOnMiss()
    {
        var stun = new BuffDefinition { Id = "晕眩", Name = "晕眩", IsDebuff = true };
        var poison = new BuffDefinition { Id = "中毒", Name = "中毒", IsDebuff = true };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            buffs: [new SkillBuffDefinition(poison, level: 1, duration: 2)],
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            talents:
            [
                CreateDamageContextTalent("段王爷的电眼", CreateOnHitConfirmedTargetStunHook(stun)),
            ],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var hitTarget = CreateUnit(
            "hit_target",
            team: 2,
            new GridPosition(1, 0),
            maxHp: 500,
            gender: CharacterGender.Female);
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, hitTarget]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.1d),
            buffResolver: buffId => string.Equals(buffId, stun.Id, StringComparison.Ordinal)
                ? stun
                : throw new InvalidOperationException($"Unexpected buff '{buffId}'."));
        engine.BeginAction(state, source.Id);

        var hitResult = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), hitTarget.Position);

        Assert.True(hitResult.Success);
        Assert.Equal(["晕眩", "中毒"], hitTarget.Buffs.Select(buff => buff.Definition.Id).ToArray());

        var missSource = CreateUnit(
            "miss_source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            talents:
            [
                CreateDamageContextTalent("段王爷的电眼", CreateOnHitConfirmedTargetStunHook(stun)),
            ],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var missTarget = CreateUnit(
            "miss_target",
            team: 2,
            new GridPosition(1, 0),
            maxHp: 500,
            talents:
            [
                CreateDamageContextTalent("易容", CreateBeforeHitCancelHook()),
            ],
            gender: CharacterGender.Female);
        missSource.ActionGauge = 100;
        var missState = new BattleState(new BattleGrid(4, 4), [missSource, missTarget]);
        var missEngine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.1d),
            buffResolver: buffId => string.Equals(buffId, stun.Id, StringComparison.Ordinal)
                ? stun
                : throw new InvalidOperationException($"Unexpected buff '{buffId}'."));
        missEngine.BeginAction(missState, missSource.Id);

        var missResult = missEngine.CastSkill(missState, missSource.Id, missSource.Character.GetExternalSkills().Single(), missTarget.Position);

        Assert.True(missResult.Success);
        Assert.Empty(missTarget.Buffs);
    }

    [Fact]
    public void CastSkill_EvasionBuffCanCauseMissBeforeHooks()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0), maxHp: 500);
        target.ApplyBuff(new BattleBuffInstance(
            new BuffDefinition
            {
                Id = "飘渺",
                Name = "飘渺",
                IsDebuff = false,
                Affixes = [new BuffLevelStatModifierAffix(StatType.Evasion, AddPerLevel: 0.07)],
            },
            level: 8,
            remainingTurns: 3,
            sourceUnitId: source.Id,
            appliedAtActionSerial: 0));
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.2d));
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.Equal(500, target.Hp);
        var damageEvent = Assert.Single(state.Events.Where(battleEvent =>
            battleEvent.Kind == BattleEventKind.Damaged &&
            battleEvent.UnitId == target.Id));
        Assert.Equal(0, damageEvent.Damage?.Amount ?? -1);
    }

    [Fact]
    public void CastSkill_AccuracyCanTurnMissBackIntoHit()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
                [StatType.Accuracy] = 1,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0), maxHp: 500);
        target.ApplyBuff(new BattleBuffInstance(
            new BuffDefinition
            {
                Id = "飘渺",
                Name = "飘渺",
                IsDebuff = false,
                Affixes = [new BuffLevelStatModifierAffix(StatType.Evasion, AddPerLevel: 0.07)],
            },
            level: 8,
            remainingTurns: 3,
            sourceUnitId: source.Id,
            appliedAtActionSerial: 0));
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.2d));
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.True(target.Hp < 500);
        Assert.DoesNotContain(state.Events, battleEvent =>
            battleEvent.Kind == BattleEventKind.Damaged &&
            battleEvent.UnitId == target.Id &&
            battleEvent.Damage?.Amount == 0);
    }

    [Fact]
    public void CastSkill_AggregatesDamageContextHookModifiersBeforeDamageRoll()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var sourceStats = new Dictionary<StatType, int>
        {
            [StatType.Quanzhang] = 100,
            [StatType.Bili] = 120,
        };
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: sourceStats,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var mixedYuan = TestContentFactory.CreateInternalSkill("混元功");
        var guardedTarget = CreateUnit(
            "guarded",
            team: 2,
            new GridPosition(1, 0),
            maxHp: 500,
            level: 5,
            talents:
            [
                CreateDamageContextTalent("金刚", CreateJingangDamageContextHook()),
                CreateDamageContextTalent("混元一气", CreateInternalSkillDefenceMultiplierHook("混元功", 1.5d)),
                CreateDamageContextTalent("宝甲", CreateFinalDamageMultiplierHook(0.95d)),
            ],
            internalSkills: [new InitialInternalSkillEntryDefinition(mixedYuan, Equipped: true)]);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var expectedContext = calculator.CreateSkillDamageContext(new BattleDamageContext(source, guardedTarget, source.Character.GetExternalSkills().Single()));
        expectedContext.AddModifier(BattleDamageContextField.TargetDefence, ModifierOp.More, 1.2d);
        expectedContext.AddModifier(BattleDamageContextField.TargetDefence, ModifierOp.PostAdd, 50d);
        expectedContext.AddModifier(BattleDamageContextField.TargetDefence, ModifierOp.More, 1.5d);
        expectedContext.AddModifier(BattleDamageContextField.FinalDamage, ModifierOp.More, 0.95d);
        var expected = calculator.CalculateSkillDamage(expectedContext).Amount;
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, guardedTarget]);
        var engine = new BattleEngine(calculator);
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), guardedTarget.Position);

        Assert.True(result.Success);
        Assert.Equal(500 - expected, guardedTarget.Hp);
    }

    [Fact]
    public void CastSkill_IshirenDoublesSourceAttackAndCriticalChanceBelowTwentyPercentHp()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var sourceStats = new Dictionary<StatType, int>
        {
            [StatType.Quanzhang] = 100,
            [StatType.Bili] = 120,
            [StatType.Fuyuan] = 250,
        };
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            maxHp: 100,
            hp: 20,
            stats: sourceStats,
            talents: [CreateIshirenTalent()],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0), maxHp: 3000);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.3d));
        var expectedContext = calculator.CreateSkillDamageContext(
            new BattleDamageContext(source, target, source.Character.GetExternalSkills().Single()));
        expectedContext.AddModifier(BattleDamageContextField.SourceAttack, ModifierOp.More, 2d);
        expectedContext.AddModifier(BattleDamageContextField.CriticalChance, ModifierOp.More, 2d);
        var expected = calculator.CalculateSkillDamage(expectedContext).Amount;
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(calculator);
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.Equal(3000 - expected, target.Hp);
    }

    [Fact]
    public void CastSkill_IshirenDoesNotTriggerAtHalfHpWithoutCaotouBaixing()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var sourceStats = new Dictionary<StatType, int>
        {
            [StatType.Quanzhang] = 100,
            [StatType.Bili] = 120,
            [StatType.Fuyuan] = 250,
        };
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            maxHp: 100,
            hp: 50,
            stats: sourceStats,
            talents: [CreateIshirenTalent()],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0), maxHp: 1000);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.3d));
        var expected = calculator.CalculateSkillDamage(
            new BattleDamageContext(source, target, source.Character.GetExternalSkills().Single())).Amount;
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(calculator);
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.Equal(1000 - expected, target.Hp);
    }

    [Fact]
    public void CastSkill_IshirenRaisesTargetDefenceUpToHalfHpWithCaotouBaixing()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var sourceStats = new Dictionary<StatType, int>
        {
            [StatType.Quanzhang] = 100,
            [StatType.Bili] = 120,
        };
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: sourceStats,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit(
            "target",
            team: 2,
            new GridPosition(1, 0),
            maxHp: 1000,
            hp: 500,
            talents:
            [
                CreateIshirenTalent(),
                new TalentDefinition
                {
                    Id = "草头百姓",
                    Name = "草头百姓",
                },
            ]);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var expectedContext = calculator.CreateSkillDamageContext(
            new BattleDamageContext(source, target, source.Character.GetExternalSkills().Single()));
        expectedContext.AddModifier(BattleDamageContextField.TargetDefence, ModifierOp.More, 1.5d);
        var expected = calculator.CalculateSkillDamage(expectedContext).Amount;
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(calculator);
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.Equal(500 - expected, target.Hp);
    }

    [Fact]
    public void CastSkill_TargetOnlyDamageContextHooksDoNotTriggerFromSource()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var sourceStats = new Dictionary<StatType, int>
        {
            [StatType.Quanzhang] = 100,
            [StatType.Bili] = 120,
        };
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: sourceStats,
            talents:
            [
                CreateDamageContextTalent("金刚", CreateTargetOnlyJingangDamageContextHook()),
                CreateDamageContextTalent("宝甲", CreateTargetOnlyFinalDamageMultiplierHook(0.95d)),
            ],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0), maxHp: 500);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var expected = calculator.CalculateSkillDamage(new BattleDamageContext(source, target, source.Character.GetExternalSkills().Single())).Amount;
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(calculator);
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.Equal(500 - expected, target.Hp);
    }

    [Fact]
    public void CastSkill_AggregatesSourceSideDamageContextHookModifiers()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "太极拳",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var sourceStats = new Dictionary<StatType, int>
        {
            [StatType.Quanzhang] = 100,
            [StatType.Bili] = 120,
        };
        var northSea = TestContentFactory.CreateInternalSkill("北冥神功");
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: sourceStats,
            talents:
            [
                CreateDamageContextTalent("北冥真气", CreateSourceInternalSkillAttackMultiplierHook("北冥神功", 1.8d)),
                CreateDamageContextTalent("太极宗师", CreateSourceSkillNameAttackAndCritHook("太极", 1.2d, 0.15d)),
                CreateDamageContextTalent("神经病", CreateSourceAttackHighMultiplierHook(1.1d)),
                CreateDamageContextTalent("鲁莽", CreateSourceAttackMultiplierHook(1.06d)),
            ],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)],
            internalSkills: [new InitialInternalSkillEntryDefinition(northSea, Equipped: true)]);
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0), maxHp: 1000);
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));
        var expectedContext = calculator.CreateSkillDamageContext(new BattleDamageContext(source, target, source.Character.GetExternalSkills().Single()));
        expectedContext.AddModifier(BattleDamageContextField.SourceAttack, ModifierOp.More, 1.8d);
        expectedContext.AddModifier(BattleDamageContextField.SourceAttack, ModifierOp.More, 1.2d);
        expectedContext.AddModifier(BattleDamageContextField.CriticalChance, ModifierOp.Add, 0.15d);
        expectedContext.AddModifier(BattleDamageContextField.SourceAttackHigh, ModifierOp.More, 1.1d);
        expectedContext.AddModifier(BattleDamageContextField.SourceAttack, ModifierOp.More, 1.06d);
        var expected = calculator.CalculateSkillDamage(expectedContext).Amount;
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(calculator);
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.Equal(1000 - expected, target.Hp);
    }

    [Fact]
    public void CastSkill_RequestsSpeechWhenBeforeSkillCastWeaponTypeHookMatches()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "独孤九剑",
            powerBase: 10,
            type: WeaponType.Jianfa,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var talent = new TalentDefinition
        {
            Id = "浪子剑客",
            Name = "浪子剑客",
            Affixes = [CreateSourceSkillWeaponTypeSpeechHook(WeaponType.Jianfa, "无招胜有招!")],
        };
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Jianfa] = 100,
                [StatType.Bili] = 120,
            },
            talents: [talent],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0));
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.05d));
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        var speech = Assert.Single(state.Events, battleEvent => battleEvent.Kind == BattleEventKind.SpeechRequested);
        Assert.Equal(source.Id, speech.UnitId);
        Assert.Equal("无招胜有招!", speech.Speech?.Text);
    }

    [Fact]
    public void CastSkill_DoesNotRequestSpeechWhenBeforeSkillCastWeaponTypeHookDoesNotMatch()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "太祖长拳",
            powerBase: 10,
            type: WeaponType.Quanzhang,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var talent = new TalentDefinition
        {
            Id = "浪子剑客",
            Name = "浪子剑客",
            Affixes = [CreateSourceSkillWeaponTypeSpeechHook(WeaponType.Jianfa, "无招胜有招!")],
        };
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            },
            talents: [talent],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0));
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.05d));
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        Assert.DoesNotContain(state.Events, battleEvent => battleEvent.Kind == BattleEventKind.SpeechRequested);
    }

    [Fact]
    public void CastSkill_AfterSkillCastWeaponTypeHookReceivesSkillContext()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "独孤九剑",
            powerBase: 10,
            type: WeaponType.Jianfa,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var talent = new TalentDefinition
        {
            Id = "浪子剑客",
            Name = "浪子剑客",
            Affixes = [CreateAfterSkillCastWeaponTypeSpeechHook(WeaponType.Jianfa, "剑随心动")],
        };
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Jianfa] = 100,
                [StatType.Bili] = 120,
            },
            talents: [talent],
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0));
        source.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(4, 4), [source, target]);
        var engine = new BattleEngine(
            new BattleDamageCalculator(new FixedRandomService(0.5d)),
            random: new FixedRandomService(0.05d));
        engine.BeginAction(state, source.Id);

        var result = engine.CastSkill(state, source.Id, source.Character.GetExternalSkills().Single(), target.Position);

        Assert.True(result.Success);
        var speech = Assert.Single(state.Events, battleEvent => battleEvent.Kind == BattleEventKind.SpeechRequested);
        Assert.Equal(HookTiming.AfterSkillCast, speech.Timing);
        Assert.Equal("剑随心动", speech.Speech?.Text);
    }

    [Fact]
    public void DamageCalculator_UsesSkillPowerStatsAndDefenceReduction()
    {
        var source = CreateUnit(
            "source",
            team: 1,
            new GridPosition(0, 0),
            stats: new Dictionary<StatType, int>
            {
                [StatType.Quanzhang] = 100,
                [StatType.Bili] = 120,
            });
        var target = CreateUnit("target", team: 2, new GridPosition(1, 0));
        var skillDefinition = TestContentFactory.CreateExternalSkill("strike", powerBase: 10);
        source.Character.SetExternalSkillState(skillDefinition, level: 1, exp: 0, active: true);
        var skill = source.Character.GetExternalSkills().Single();
        var calculator = new BattleDamageCalculator(new FixedRandomService(0.5d));

        var result = calculator.CalculateSkillDamage(new BattleDamageContext(source, target, skill));

        var attack = 10d * (2d + 100d / 200d) * 2.5d * (4d + 120d / 120d);
        var defence = 150d + 10d * 8d;
        var expected = (int)(attack * (1d - BattleDamageCalculator.CalculateDefenceReduction(defence)));
        Assert.Equal(expected, result.Amount);
        Assert.False(result.IsCritical);
        Assert.Equal(0d, result.CriticalChance);
    }

    [Fact]
    public void UseItem_CanTargetAllyOnlyWithTraitAndRange()
    {
        var talent = new TalentDefinition
        {
            Id = "ally_item",
            Name = "ally_item",
            Affixes = [new TraitAffix(TraitId.CanUseItemOnAlly)],
        };
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0), talents: [talent]);
        var ally = CreateUnit("ally", team: 1, new GridPosition(2, 0), maxMp: 20, mp: 5);
        hero.ActionGauge = 100;
        var item = new NormalItemDefinition
        {
            Id = "mp_pill",
            Name = "mp_pill",
            Type = ItemType.Consumable,
            UseEffects = [new AddMpItemUseEffectDefinition(10)],
        };
        var state = new BattleState(new BattleGrid(5, 5), [hero, ally]);
        var engine = new BattleEngine();
        engine.BeginAction(state, hero.Id);

        var result = engine.UseItem(state, hero.Id, item, ally.Id);

        Assert.True(result.Success);
        Assert.Equal(15, ally.Mp);
        Assert.Null(state.CurrentAction);
    }

    [Fact]
    public void UseItem_AddsTargetItemCooldownAndBlocksRegularReuse()
    {
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0), maxMp: 20, mp: 5);
        hero.ActionGauge = 100;
        var item = new NormalItemDefinition
        {
            Id = "mp_pill",
            Name = "mp_pill",
            Type = ItemType.Consumable,
            Cooldown = 2,
            UseEffects = [new AddMpItemUseEffectDefinition(1)],
        };
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var engine = new BattleEngine();
        engine.BeginAction(state, hero.Id);

        var first = engine.UseItem(state, hero.Id, item, hero.Id);

        Assert.True(first.Success);
        Assert.Equal(2, hero.ItemCooldown);

        hero.ActionGauge = 100;
        engine.BeginAction(state, hero.Id);
        var second = engine.UseItem(state, hero.Id, item, hero.Id);

        Assert.False(second.Success);
        Assert.Equal("Item is cooling down. Remaining turns: 2.", second.Message);
    }

    [Fact]
    public void UseItem_CannotBypassTargetItemCooldownWithOnlyAllyItemTrait()
    {
        var talent = new TalentDefinition
        {
            Id = "ally_item",
            Name = "ally_item",
            Affixes = [new TraitAffix(TraitId.CanUseItemOnAlly)],
        };
        var healer = CreateUnit("healer", team: 1, new GridPosition(0, 0), talents: [talent]);
        var target = CreateUnit("target", team: 1, new GridPosition(1, 0), maxMp: 20, mp: 5);
        target.AddItemCooldown(2);
        healer.ActionGauge = 100;
        var item = new NormalItemDefinition
        {
            Id = "mp_pill",
            Name = "mp_pill",
            Type = ItemType.Consumable,
            Cooldown = 1,
            UseEffects = [new AddMpItemUseEffectDefinition(1)],
        };
        var state = new BattleState(new BattleGrid(4, 4), [healer, target]);
        var engine = new BattleEngine();
        engine.BeginAction(state, healer.Id);

        var result = engine.UseItem(state, healer.Id, item, target.Id);

        Assert.False(result.Success);
        Assert.Equal("Item is cooling down. Remaining turns: 2.", result.Message);
        Assert.Equal(2, target.ItemCooldown);
    }

    [Fact]
    public void UseItem_CanBypassTargetItemCooldownWithIgnoreItemCooldownTrait()
    {
        var talent = new TalentDefinition
        {
            Id = "ignore_item_cd",
            Name = "ignore_item_cd",
            Affixes = [new TraitAffix(TraitId.IgnoreItemCooldown)],
        };
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0), talents: [talent], maxMp: 20, mp: 5);
        hero.AddItemCooldown(2);
        hero.ActionGauge = 100;
        var item = new NormalItemDefinition
        {
            Id = "mp_pill",
            Name = "mp_pill",
            Type = ItemType.Consumable,
            Cooldown = 1,
            UseEffects = [new AddMpItemUseEffectDefinition(1)],
        };
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var engine = new BattleEngine();
        engine.BeginAction(state, hero.Id);

        var result = engine.UseItem(state, hero.Id, item, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(3, hero.ItemCooldown);
    }

    [Fact]
    public void AdvanceUntilNextAction_ReducesItemCooldownEveryTimelineRound()
    {
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0), actionSpeed: 1);
        hero.AddItemCooldown(2);
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var engine = new BattleEngine();

        Assert.Throws<InvalidOperationException>(() => engine.AdvanceUntilNextAction(state, maxTicks: 50));

        Assert.Equal(1, hero.ItemCooldown);
    }

    [Fact]
    public void UseItem_AppliesBuffWithConfiguredLevelAndDuration()
    {
        var drunk = new BuffDefinition { Id = "醉酒", Name = "醉酒", IsDebuff = false };
        var hero = CreateUnit("hero", team: 1, new GridPosition(0, 0));
        hero.ActionGauge = 100;
        var item = new NormalItemDefinition
        {
            Id = "wine",
            Name = "wine",
            Type = ItemType.Consumable,
            UseEffects = [new AddBuffItemUseEffectDefinition("醉酒", Level: 0, Duration: 3)],
        };
        var state = new BattleState(new BattleGrid(4, 4), [hero]);
        var engine = new BattleEngine(buffResolver: id => id == drunk.Id ? drunk : throw new KeyNotFoundException(id));
        engine.BeginAction(state, hero.Id);

        var result = engine.UseItem(state, hero.Id, item, hero.Id);

        Assert.True(result.Success);
        var buff = Assert.Single(hero.Buffs);
        Assert.Equal("醉酒", buff.Definition.Id);
        Assert.Equal(0, buff.Level);
        Assert.Equal(3, buff.RemainingTurns);
    }

    private static HookAffix CreateInternalInjuryCostHook() =>
        new()
        {
            Timing = HookTiming.BeforeSkillCost,
            Effects =
            [
                new ModifyMpCostBattleHookEffectDefinition(ModifierOp.Increase, DeltaPerBuffLevel: 0.1d),
            ],
        };

    private static HookAffix CreatePoisonMasteryHook() =>
        new()
        {
            Timing = HookTiming.BeforeBuffApplied,
            Conditions =
            [
                new ContextBuffIdBattleHookConditionDefinition("中毒"),
            ],
            Effects =
            [
                new StrengthenContextBuffBattleHookEffectDefinition(LevelDelta: 3, TurnDelta: 2),
            ],
        };

    private static HookAffix CreatePuzhaoHook() =>
        new()
        {
            Timing = HookTiming.BeforeActionStart,
            Conditions =
            [
                new ChanceBattleHookConditionDefinition(0.4d),
            ],
            Effects =
            [
                new ApplyBuffBattleEffectDefinition(
                    new NearbyAlliesBattleTargetSelectorDefinition(Radius: 2, IncludeSelf: true),
                    "恢复",
                    Level: 2,
                    Duration: 3),
            ],
        };

    private static HookAffix CreateQiankunShiftHook() =>
        new()
        {
            Timing = HookTiming.OnDamageTaken,
            Conditions =
            [
                new DamagePositiveBattleHookConditionDefinition(),
                new ChanceBattleHookConditionDefinition(0.5d),
            ],
            Effects =
            [
                new ModifyDamageBattleHookEffectDefinition(ModifierOp.Increase, Delta: -0.5d),
            ],
        };

    private static HookAffix CreateBeforeHitCancelHook(bool suppressHitEffects = true) =>
        new()
        {
            Timing = HookTiming.BeforeHitResolved,
            Conditions =
            [
                new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Target),
            ],
            Effects =
            [
                new CancelHitBattleHookEffectDefinition(suppressHitEffects),
            ],
        };

    private static HookAffix CreateSetHitSuccessWhenMissHook() =>
        new()
        {
            Timing = HookTiming.BeforeHitResolved,
            Conditions =
            [
                new ContextHitStateBattleHookConditionDefinition(BattleHitState.Miss),
            ],
            Effects =
            [
                new SetHitSuccessBattleHookEffectDefinition(),
            ],
        };

    private static HookAffix CreateSourceFumbleHook() =>
        new()
        {
            Timing = HookTiming.BeforeHitResolved,
            Conditions =
            [
                new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Source),
                new ChanceBattleHookConditionDefinition(1d),
            ],
            Effects =
            [
                new CancelHitBattleHookEffectDefinition(),
            ],
        };

    private static HookAffix CreateFemaleOrEunuchMissHook() =>
        new()
        {
            Timing = HookTiming.BeforeHitResolved,
            Conditions =
            [
                new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Target),
                new ContextUnitRelationBattleHookConditionDefinition(BattleHookContextUnitRole.Source, BattleHookRelation.Enemy),
                new ContextUnitGenderBattleHookConditionDefinition(BattleHookContextUnitRole.Source, [CharacterGender.Female, CharacterGender.Eunuch]),
            ],
            Effects =
            [
                new CancelHitBattleHookEffectDefinition(),
            ],
        };

    private static HookAffix CreateOnHitConfirmedTargetStunHook(BuffDefinition stun) =>
        new()
        {
            Timing = HookTiming.OnHitConfirmed,
            Conditions =
            [
                new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Source),
                new ContextUnitGenderBattleHookConditionDefinition(BattleHookContextUnitRole.Target, [CharacterGender.Female, CharacterGender.Eunuch]),
                new ChanceBattleHookConditionDefinition(1d),
            ],
            Effects =
            [
                new ApplyBuffBattleEffectDefinition(
                    new TargetBattleTargetSelectorDefinition(),
                    stun.Id,
                    Level: 0,
                    Duration: 2,
                    Chance: 100),
            ],
        };

    private static TalentDefinition CreateIshirenTalent() =>
        new()
        {
            Id = "异世人",
            Name = "异世人",
            Affixes =
            [
                CreateIshirenSourceHook(maxInclusive: 0.2d),
                CreateIshirenSourceHook(minExclusive: 0.2d, maxInclusive: 0.5d, requiredTalentId: "草头百姓"),
                CreateIshirenTargetHook(maxInclusive: 0.2d),
                CreateIshirenTargetHook(minExclusive: 0.2d, maxInclusive: 0.5d, requiredTalentId: "草头百姓"),
            ],
        };

    private static TalentDefinition CreateDamageContextTalent(string id, HookAffix hook) =>
        new()
        {
            Id = id,
            Name = id,
            Affixes = [hook],
        };

    private static HookAffix CreateJingangDamageContextHook() =>
        new()
        {
            Timing = HookTiming.BeforeDamageCalculation,
            Effects =
            [
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.TargetDefence,
                    ModifierOp.More,
                    1.2d),
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.TargetDefence,
                    ModifierOp.PostAdd,
                    0d,
                    DeltaPerUnitLevel: 10d),
            ],
        };

    private static HookAffix CreateTargetOnlyJingangDamageContextHook() =>
        CreateTargetOnly(CreateJingangDamageContextHook());

    private static HookAffix CreateInternalSkillDefenceMultiplierHook(string internalSkillId, double multiplier) =>
        new()
        {
            Timing = HookTiming.BeforeDamageCalculation,
            Conditions =
            [
                new ContextUnitEquippedInternalSkillBattleHookConditionDefinition([internalSkillId]),
            ],
            Effects =
            [
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.TargetDefence,
                    ModifierOp.More,
                    multiplier),
            ],
        };

    private static HookAffix CreateFinalDamageMultiplierHook(double multiplier) =>
        new()
        {
            Timing = HookTiming.BeforeDamageCalculation,
            Effects =
            [
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.FinalDamage,
                    ModifierOp.More,
                    multiplier),
            ],
        };

    private static HookAffix CreateTargetOnlyFinalDamageMultiplierHook(double multiplier) =>
        CreateTargetOnly(CreateFinalDamageMultiplierHook(multiplier));

    private static HookAffix CreateTargetOnly(HookAffix hook) =>
        hook with
        {
            Conditions = hook.Conditions.Prepend(new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Target)).ToList(),
        };

    private static HookAffix CreateSourceInternalSkillAttackMultiplierHook(string internalSkillId, double multiplier) =>
        new()
        {
            Timing = HookTiming.BeforeDamageCalculation,
            Conditions =
            [
                new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Source),
                new ContextUnitEquippedInternalSkillBattleHookConditionDefinition([internalSkillId]),
            ],
            Effects =
            [
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.SourceAttack,
                    ModifierOp.More,
                    multiplier),
            ],
        };

    private static HookAffix CreateSourceSkillNameAttackAndCritHook(
        string skillNameFragment,
        double attackMultiplier,
        double criticalChanceDelta) =>
        new()
        {
            Timing = HookTiming.BeforeDamageCalculation,
            Conditions =
            [
                new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Source),
                new ContextSkillNameContainsBattleHookConditionDefinition([skillNameFragment]),
            ],
            Effects =
            [
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.SourceAttack,
                    ModifierOp.More,
                    attackMultiplier),
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.CriticalChance,
                    ModifierOp.Add,
                    criticalChanceDelta),
            ],
        };

    private static HookAffix CreateSourceSkillWeaponTypeSpeechHook(WeaponType weaponType, string line) =>
        new()
        {
            Timing = HookTiming.BeforeSkillCast,
            Conditions =
            [
                new ContextSkillWeaponTypeBattleHookConditionDefinition([weaponType]),
            ],
            Speech = new BattleSpeechDefinition
            {
                Lines = [line], Chance= 0.1d
            }
        };

    private static HookAffix CreateAfterSkillCastWeaponTypeSpeechHook(WeaponType weaponType, string line) =>
        CreateSourceSkillWeaponTypeSpeechHook(weaponType, line) with
        {
            Timing = HookTiming.AfterSkillCast,
        };

    private static HookAffix CreateSourceAttackHighMultiplierHook(double multiplier) =>
        new()
        {
            Timing = HookTiming.BeforeDamageCalculation,
            Conditions =
            [
                new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Source),
            ],
            Effects =
            [
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.SourceAttackHigh,
                    ModifierOp.More,
                    multiplier),
            ],
        };

    private static HookAffix CreateSourceAttackMultiplierHook(double multiplier) =>
        new()
        {
            Timing = HookTiming.BeforeDamageCalculation,
            Conditions =
            [
                new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Source),
            ],
            Effects =
            [
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.SourceAttack,
                    ModifierOp.More,
                    multiplier),
            ],
        };

    private static HookAffix CreateIshirenSourceHook(
        double? minExclusive = null,
        double? maxInclusive = null,
        string? requiredTalentId = null)
    {
        var conditions = new List<BattleHookConditionDefinition>
        {
            new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Source),
            new ContextUnitHpRatioBattleHookConditionDefinition(minExclusive, maxInclusive),
        };
        if (!string.IsNullOrWhiteSpace(requiredTalentId))
        {
            conditions.Add(new ContextUnitEffectiveTalentBattleHookConditionDefinition([requiredTalentId]));
        }

        return new HookAffix
        {
            Timing = HookTiming.BeforeDamageCalculation,
            Conditions = conditions,
            Effects =
            [
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.SourceAttack,
                    ModifierOp.More,
                    2d),
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.CriticalChance,
                    ModifierOp.More,
                    2d),
            ],
        };
    }

    private static HookAffix CreateIshirenTargetHook(
        double? minExclusive = null,
        double? maxInclusive = null,
        string? requiredTalentId = null)
    {
        var conditions = new List<BattleHookConditionDefinition>
        {
            new ContextUnitRoleBattleHookConditionDefinition(BattleHookContextUnitRole.Target),
            new ContextUnitHpRatioBattleHookConditionDefinition(minExclusive, maxInclusive),
        };
        if (!string.IsNullOrWhiteSpace(requiredTalentId))
        {
            conditions.Add(new ContextUnitEffectiveTalentBattleHookConditionDefinition([requiredTalentId]));
        }

        return new HookAffix
        {
            Timing = HookTiming.BeforeDamageCalculation,
            Conditions = conditions,
            Effects =
            [
                new ModifyDamageContextBattleHookEffectDefinition(
                    BattleDamageContextField.TargetDefence,
                    ModifierOp.More,
                    1.5d),
            ],
        };
    }

    private static BattleUnit CreateUnit(
        string id,
        int team,
        GridPosition position,
        int maxHp = 100,
        int maxMp = 30,
        int? hp = null,
        int? mp = null,
        int rage = 0,
        double? actionSpeed = 10,
        int? movePower = 5,
        IReadOnlyDictionary<StatType, int>? stats = null,
        IReadOnlyList<TalentDefinition>? talents = null,
        IReadOnlyList<InitialExternalSkillEntryDefinition>? externalSkills = null,
        IReadOnlyList<InitialInternalSkillEntryDefinition>? internalSkills = null,
        int level = 1,
        CharacterGender gender = CharacterGender.Neutral)
    {
        var mergedStats = new Dictionary<StatType, int>
        {
            [StatType.MaxHp] = maxHp,
            [StatType.MaxMp] = maxMp,
        };
        if (actionSpeed is not null)
        {
            mergedStats[StatType.Speed] = (int)Math.Round(actionSpeed.Value, MidpointRounding.AwayFromZero);
        }

        if (movePower is not null)
        {
            mergedStats[StatType.Movement] = Math.Max(0, movePower.Value - 2);
        }

        foreach (var (stat, value) in stats ?? new Dictionary<StatType, int>())
        {
            mergedStats[stat] = value;
        }

        var definition = TestContentFactory.CreateCharacterDefinition(
            id,
            stats: mergedStats,
            externalSkills: externalSkills,
            internalSkills: internalSkills,
            talents: talents,
            level: level,
            gender: gender);
        var character = TestContentFactory.CreateCharacterInstance(id, definition);
        return new BattleUnit(
            id,
            character,
            team,
            position,
            maxHp: maxHp,
            maxMp: maxMp,
            hp: hp,
            mp: mp,
            rage: rage);
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
