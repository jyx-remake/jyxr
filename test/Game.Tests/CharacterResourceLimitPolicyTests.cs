using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Story;

namespace Game.Tests;

public sealed class CharacterResourceLimitPolicyTests
{
    [Fact]
    public void GetMaxHpMp_AddsRoundBonus()
    {
        var config = new GameConfig
        {
            MaxHpMp = 100,
            MaxHpMpPerRound = 25,
        };

        Assert.Equal(100, new CharacterResourceLimitPolicy(config, round: 1).GetMaxHpMp());
        Assert.Equal(125, new CharacterResourceLimitPolicy(config, round: 2).GetMaxHpMp());
        Assert.Equal(175, new CharacterResourceLimitPolicy(config, round: 4).GetMaxHpMp());
    }

    [Fact]
    public void GainExperience_ClampsMaxHpAndMaxMpToCurrentRoundLimit()
    {
        var growTemplate = TestContentFactory.CreateGrowTemplate(
            CharacterExperienceProgression.DefaultGrowTemplateId,
            new Dictionary<StatType, int>
            {
                [StatType.MaxHp] = 20,
                [StatType.MaxMp] = 30,
            });
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.MaxHp] = 120,
                [StatType.MaxMp] = 110,
            });
        var state = CreateStateWithHero(heroDefinition, out var hero);
        hero.RestoreBattleResources();
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            growTemplates: [growTemplate]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                MaxHpMp = 100,
                MaxHpMpPerRound = 50,
            });

        session.CharacterService.GainExperience("hero", CharacterLevelProgression.GetLevelUpExperience(1));

        Assert.Equal(100, hero.GetBaseStat(StatType.MaxHp));
        Assert.Equal(100, hero.GetBaseStat(StatType.MaxMp));
        Assert.Equal(100, hero.CurrentHp);
        Assert.Equal(100, hero.CurrentMp);
    }

    [Fact]
    public async Task StoryUpgrade_ClampsMaxHpAndMaxMpToCurrentRoundLimit()
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.MaxHp] = 140,
                [StatType.MaxMp] = 135,
            });
        var script = new StoryScript(
            StoryScript.CurrentVersion,
            [
                new Segment(
                    "resource_boost",
                    [
                        new CommandStep(
                            "upgrade",
                            [
                                new LiteralExprNode(ExprValue.FromString("maxhp")),
                                new LiteralExprNode(ExprValue.FromString("hero")),
                                new LiteralExprNode(ExprValue.FromNumber(20)),
                            ]),
                        new CommandStep(
                            "upgrade",
                            [
                                new LiteralExprNode(ExprValue.FromString("maxmp")),
                                new LiteralExprNode(ExprValue.FromString("hero")),
                                new LiteralExprNode(ExprValue.FromNumber(30)),
                            ]),
                    ]),
            ]);
        var state = CreateStateWithHero(heroDefinition, out var hero);
        hero.RestoreBattleResources();
        state.Adventure.SetRound(2);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            storyScripts: [script]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                MaxHpMp = 100,
                MaxHpMpPerRound = 25,
            });

        await session.StoryService.ExecuteAsync("resource_boost");

        Assert.Equal(125, hero.GetBaseStat(StatType.MaxHp));
        Assert.Equal(125, hero.GetBaseStat(StatType.MaxMp));
        Assert.Equal(125, hero.CurrentHp);
        Assert.Equal(125, hero.CurrentMp);
    }

    [Fact]
    public void Booster_ClampsMaxHpAndMaxMpToCurrentRoundLimit()
    {
        var booster = new NormalItemDefinition
        {
            Id = "peach",
            Name = "peach",
            Type = ItemType.Booster,
            UseEffects =
            [
                new AddMaxHpItemUseEffectDefinition(100),
                new AddMaxMpItemUseEffectDefinition(100),
            ],
        };
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            new Dictionary<StatType, int>
            {
                [StatType.MaxHp] = 160,
                [StatType.MaxMp] = 155,
            });
        var state = CreateStateWithHero(heroDefinition, out var hero);
        hero.RestoreBattleResources();
        state.Adventure.SetRound(2);
        state.Inventory.AddItem(booster);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            items: [booster]);
        var session = new GameSession(
            state,
            repository,
            config: new GameConfig
            {
                MaxHpMp = 100,
                MaxHpMpPerRound = 50,
            });
        var entry = state.Inventory.GetStack(booster);

        var result = session.ItemUseService.Use(entry, hero.Id);

        Assert.True(result.Success);
        Assert.Equal(150, hero.GetBaseStat(StatType.MaxHp));
        Assert.Equal(150, hero.GetBaseStat(StatType.MaxMp));
        Assert.Equal(150, hero.CurrentHp);
        Assert.Equal(150, hero.CurrentMp);
    }

    private static GameState CreateStateWithHero(
        CharacterDefinition heroDefinition,
        out CharacterInstance hero)
    {
        var state = new GameState();
        hero = TestContentFactory.CreateCharacterInstance("hero", heroDefinition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        return state;
    }
}
