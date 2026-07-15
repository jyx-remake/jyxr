using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Persistence;
using Game.Core.Serialization;

namespace Game.Tests;

public sealed class BattleAiTests
{
    [Fact]
    public void BattleUnit_CopiesAiTypeFromCharacter()
    {
        var unit = CreateUnit("hero", team: 1, new GridPosition(1, 1), aiType: BattleAiType.Training);

        Assert.Equal(BattleAiType.Training, unit.AiType);
    }

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
        var agent = CreateAgent(engine);

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
        var agent = CreateAgent(engine);

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.CastSkill, plan.MainAction.Kind);
        Assert.Equal(new GridPosition(4, 1), plan.MainAction.TargetPosition);
    }

    [Fact]
    public void Decide_DoesNotChooseSkill_WhenResolvedMpCostExceedsCurrentMp()
    {
        var internalInjury = new BuffDefinition
        {
            Id = "内伤",
            Name = "内伤",
            IsDebuff = true,
            Affixes =
            [
                new HookAffix
                {
                    Timing = HookTiming.BeforeSkillCost,
                    Effects =
                    [
                        new ModifyMpCostBattleHookEffectDefinition(ModifierOp.Increase, DeltaPerBuffLevel: 0.1d),
                    ],
                },
            ],
        };
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            mpCost: 10,
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var enemy = CreateUnit(
            "enemy",
            team: 2,
            new GridPosition(1, 1),
            maxMp: 30,
            mp: 11,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var player = CreateUnit("player", team: 1, new GridPosition(2, 1));
        enemy.TryApplyBuff(new BattleBuffInstance(internalInjury, level: 2, remainingTurns: 3, player.Id, 1));
        enemy.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [enemy, player]);
        var engine = new BattleEngine();
        engine.BeginAction(state, enemy.Id);
        var agent = CreateAgent(engine);

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.Rest, plan.MainAction.Kind);
    }

    [Fact]
    public void Decide_TrainingPrefersUnmaxedSkillOverMaxedSkill()
    {
        var maxedSkill = TestContentFactory.CreateExternalSkill(
            "maxed",
            powerBase: 200,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var unmaxedSkill = TestContentFactory.CreateExternalSkill(
            "unmaxed",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var enemy = CreateUnit(
            "enemy",
            team: 2,
            new GridPosition(1, 1),
            aiType: BattleAiType.Training,
            externalSkills:
            [
                new InitialExternalSkillEntryDefinition(maxedSkill, 10),
                new InitialExternalSkillEntryDefinition(unmaxedSkill, 1),
            ]);
        var player = CreateUnit("player", team: 1, new GridPosition(2, 1));
        enemy.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [enemy, player]);
        var engine = new BattleEngine();
        engine.BeginAction(state, enemy.Id);
        var agent = CreateAgent(engine);

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.CastSkill, plan.MainAction.Kind);
        Assert.Equal("unmaxed", plan.MainAction.SkillId);
    }

    [Fact]
    public void Decide_TrainingUsesBasicBehaviorWhenAllProgressionSkillsAreMaxed()
    {
        var strongSkill = TestContentFactory.CreateExternalSkill(
            "strong",
            powerBase: 200,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var weakSkill = TestContentFactory.CreateExternalSkill(
            "weak",
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var enemy = CreateUnit(
            "enemy",
            team: 2,
            new GridPosition(1, 1),
            aiType: BattleAiType.Training,
            externalSkills:
            [
                new InitialExternalSkillEntryDefinition(strongSkill, 10),
                new InitialExternalSkillEntryDefinition(weakSkill, 10),
            ]);
        var player = CreateUnit("player", team: 1, new GridPosition(2, 1));
        enemy.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [enemy, player]);
        var engine = new BattleEngine();
        engine.BeginAction(state, enemy.Id);
        var agent = CreateAgent(engine);

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.CastSkill, plan.MainAction.Kind);
        Assert.Equal("strong", plan.MainAction.SkillId);
    }

    [Fact]
    public void Decide_TrainingRestsInsteadOfUsingMaxedSkillWhenUnmaxedSkillIsUnavailable()
    {
        var maxedSkill = TestContentFactory.CreateExternalSkill(
            "maxed",
            powerBase: 200,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var unmaxedSkill = TestContentFactory.CreateExternalSkill(
            "unmaxed",
            mpCost: 99,
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var enemy = CreateUnit(
            "enemy",
            team: 2,
            new GridPosition(1, 1),
            aiType: BattleAiType.Training,
            maxMp: 30,
            mp: 30,
            externalSkills:
            [
                new InitialExternalSkillEntryDefinition(maxedSkill, 10),
                new InitialExternalSkillEntryDefinition(unmaxedSkill, 1),
            ]);
        var player = CreateUnit("player", team: 1, new GridPosition(2, 1));
        enemy.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [enemy, player]);
        var engine = new BattleEngine();
        engine.BeginAction(state, enemy.Id);
        var agent = CreateAgent(engine);

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.Rest, plan.MainAction.Kind);
    }

    [Fact]
    public void Decide_TrainingIgnoresFormSkillsWhenCheckingForUnmaxedProgressionSkills()
    {
        var maxedSkill = TestContentFactory.CreateExternalSkill(
            "maxed",
            powerBase: 200,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var unmaxedSkill = TestContentFactory.CreateExternalSkill(
            "unmaxed",
            formSkills:
            [
                new FormSkillDefinition(
                    "unmaxed_form",
                    "unmaxed_form",
                    string.Empty,
                    null,
                    UnlockLevel: 1,
                    Cooldown: 0,
                    Cost: new SkillCostDefinition(Rage: 99),
                    Targeting: new SkillTargetingDefinition(CastSize: 3, ImpactType: SkillImpactType.Single, ImpactSize: 0),
                    PowerExtra: 10,
                    string.Empty,
                    string.Empty,
                    [])
            ],
            powerBase: 10,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var enemy = CreateUnit(
            "enemy",
            team: 2,
            new GridPosition(1, 1),
            aiType: BattleAiType.Training,
            externalSkills:
            [
                new InitialExternalSkillEntryDefinition(maxedSkill, 10),
                new InitialExternalSkillEntryDefinition(unmaxedSkill, 1),
            ]);
        enemy.Character.SetExternalSkillActive("unmaxed", false);
        var player = CreateUnit("player", team: 1, new GridPosition(2, 1));
        enemy.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [enemy, player]);
        var engine = new BattleEngine();
        engine.BeginAction(state, enemy.Id);
        var agent = CreateAgent(engine);

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.CastSkill, plan.MainAction.Kind);
        Assert.Equal("maxed", plan.MainAction.SkillId);
    }

    [Fact]
    public void Decide_AttackOnlyChoosesAttackWhenBasicWouldRest()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "poke",
            powerBase: 1,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var enemy = CreateUnit(
            "enemy",
            team: 2,
            new GridPosition(1, 1),
            maxHp: 100,
            hp: 20,
            aiType: BattleAiType.AttackOnly,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var player = CreateUnit("player", team: 1, new GridPosition(2, 1));
        enemy.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [enemy, player]);
        var engine = new BattleEngine();
        engine.BeginAction(state, enemy.Id);
        var agent = CreateAgent(engine);

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.CastSkill, plan.MainAction.Kind);
        Assert.Equal("poke", plan.MainAction.SkillId);
    }

    [Fact]
    public void Decide_AttackOnlyAllowsMoveAndRestWhenNoAttackCandidateExists()
    {
        var enemy = CreateUnit(
            "enemy",
            team: 2,
            new GridPosition(1, 1),
            aiType: BattleAiType.AttackOnly);
        var player = CreateUnit("player", team: 1, new GridPosition(4, 1));
        enemy.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [enemy, player]);
        var engine = new BattleEngine();
        engine.BeginAction(state, enemy.Id);
        var agent = CreateAgent(engine);

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.Rest, plan.MainAction.Kind);
        Assert.NotEqual(enemy.Position, plan.MoveDestination);
    }

    [Fact]
    public void Decide_RestOnlyRestsInPlaceEvenWhenAttackIsAvailable()
    {
        var skillDefinition = TestContentFactory.CreateExternalSkill(
            "strike",
            powerBase: 200,
            impactType: SkillImpactType.Single,
            impactSize: 0,
            castSize: 3);
        var enemy = CreateUnit(
            "enemy",
            team: 2,
            new GridPosition(1, 1),
            aiType: BattleAiType.RestOnly,
            externalSkills: [new InitialExternalSkillEntryDefinition(skillDefinition, 1)]);
        var player = CreateUnit("player", team: 1, new GridPosition(2, 1));
        enemy.ActionGauge = 100;
        var state = new BattleState(new BattleGrid(6, 6), [enemy, player]);
        var engine = new BattleEngine();
        engine.BeginAction(state, enemy.Id);
        var agent = CreateAgent(engine);

        var plan = agent.Decide(state, enemy.Id);

        Assert.Equal(BattleMainActionKind.Rest, plan.MainAction.Kind);
        Assert.Equal(enemy.Position, plan.MoveDestination);
    }

    [Fact]
    public void CharacterRecord_RoundTripsAiTypeAndDefaultsLegacyRecordToBasic()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var character = TestContentFactory.CreateCharacterInstance("hero", definition);
        character.SetAiType(BattleAiType.AttackOnly);
        var record = CharacterMapper.ToRecord(character);

        var json = JsonSerializer.Serialize(record, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<CharacterRecord>(json, GameJson.Default);
        var legacyNode = JsonNode.Parse(json)!.AsObject();
        legacyNode.Remove(nameof(CharacterRecord.AiType));
        var legacyRecord = JsonSerializer.Deserialize<CharacterRecord>(
            legacyNode.ToJsonString(GameJson.Default),
            GameJson.Default);

        Assert.NotNull(roundTripped);
        Assert.Equal(BattleAiType.AttackOnly, roundTripped.AiType);
        Assert.NotNull(legacyRecord);
        Assert.Equal(BattleAiType.Basic, legacyRecord.AiType);
    }

    private static BattleUnit CreateUnit(
        string id,
        int team,
        GridPosition position,
        int maxHp = 100,
        int maxMp = 30,
        int? hp = null,
        int? mp = null,
        BattleAiType aiType = BattleAiType.Basic,
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
        character.SetAiType(aiType);
        return new BattleUnit(id, character, team, position, hp: hp, mp: mp);
    }

    private static BasicEnemyBattleAgent CreateAgent(BattleEngine engine) =>
        new(new BattleTurnCandidateGenerator(engine), new BattleAiPolicyResolver(_ => 10));
}
