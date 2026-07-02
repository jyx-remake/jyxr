using Game.Application;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Tests;

public sealed class NewGameStateFactoryTests
{
    [Fact]
    public void Create_InitializesPartyMembersInConfiguredOrderAndMaxesSkills()
    {
        var slash = TestContentFactory.CreateExternalSkill("slash");
        var breath = TestContentFactory.CreateInternalSkill("breath");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(slash, Level: 1)],
            internalSkills: [new InitialInternalSkillEntryDefinition(breath, Level: 2)]);
        var allyDefinition = TestContentFactory.CreateCharacterDefinition("ally");
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition, allyDefinition],
            externalSkills: [slash],
            internalSkills: [breath]);
        var config = new GameConfig
        {
            BaseExternalSkillMaxLevel = 7,
            BaseInternalSkillMaxLevel = 9,
        };

        var state = new NewGameStateFactory(repository, config).Create(["hero", "ally"]);

        Assert.Equal(["hero", "ally"], state.Party.Members.Select(member => member.Id).ToArray());
        var hero = state.Party.GetMember("hero");
        var externalSkill = Assert.Single(hero.ExternalSkills);
        var internalSkill = Assert.Single(hero.InternalSkills);
        Assert.Equal(7, externalSkill.Level);
        Assert.Equal(9, internalSkill.Level);
    }

    [Fact]
    public void Create_KeepsDefinitionSkillLevelsWhenConfiguredNotToMaximizeNewPartyCharacterSkills()
    {
        var slash = TestContentFactory.CreateExternalSkill("slash");
        var breath = TestContentFactory.CreateInternalSkill("breath");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(slash, Level: 3)],
            internalSkills: [new InitialInternalSkillEntryDefinition(breath, Level: 4)]);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [slash],
            internalSkills: [breath]);
        var config = new GameConfig
        {
            MaximizeNewPartyCharacterSkills = false,
            BaseExternalSkillMaxLevel = 7,
            BaseInternalSkillMaxLevel = 9,
        };

        var state = new NewGameStateFactory(repository, config).Create(["hero"]);

        var hero = state.Party.GetMember("hero");
        var externalSkill = Assert.Single(hero.ExternalSkills);
        var internalSkill = Assert.Single(hero.InternalSkills);
        Assert.Equal(3, externalSkill.Level);
        Assert.Equal(4, internalSkill.Level);
    }

    [Fact]
    public void PartyServiceJoin_UsesSameInitialCharacterCreationRules()
    {
        var slash = TestContentFactory.CreateExternalSkill("slash");
        var breath = TestContentFactory.CreateInternalSkill("breath");
        var allyDefinition = TestContentFactory.CreateCharacterDefinition(
            "ally",
            externalSkills: [new InitialExternalSkillEntryDefinition(slash, Level: 1)],
            internalSkills: [new InitialInternalSkillEntryDefinition(breath, Level: 2)]);
        var repository = TestContentFactory.CreateRepository(
            characters: [allyDefinition],
            externalSkills: [slash],
            internalSkills: [breath]);
        var config = new GameConfig
        {
            BaseExternalSkillMaxLevel = 7,
            BaseInternalSkillMaxLevel = 9,
        };
        var session = new GameSession(new GameState(), repository, config: config);

        session.PartyService.Join("ally");

        var ally = Assert.Single(session.State.Party.Members);
        Assert.Equal("ally", ally.Id);
        Assert.Equal(7, Assert.Single(ally.ExternalSkills).Level);
        Assert.Equal(9, Assert.Single(ally.InternalSkills).Level);
    }

    [Fact]
    public void Create_MaxesSkillsUsingProfileBonus()
    {
        var slash = TestContentFactory.CreateExternalSkill("slash");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(slash, Level: 1)]);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [slash]);
        var profile = new GameProfile();
        profile.AddSkillMaxLevelBonus("slash", 3);
        var policy = new SkillMaxLevelPolicy(profile: profile);

        var state = new NewGameStateFactory(repository, skillMaxLevelPolicy: policy).Create(["hero"]);

        var skill = Assert.Single(state.Party.GetMember("hero").ExternalSkills);
        Assert.Equal(13, skill.Level);
    }

    [Fact]
    public void Create_MaxesSkillsUsingTargetRoundBonus()
    {
        var slash = TestContentFactory.CreateExternalSkill("slash");
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            "hero",
            externalSkills: [new InitialExternalSkillEntryDefinition(slash, Level: 1)]);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            externalSkills: [slash]);
        var config = new GameConfig
        {
            BaseExternalSkillMaxLevel = 10,
            RoundsPerMaxSkillLevelIncrease = 2,
            AbsoluteSkillMaxLevel = 20,
        };

        var state = new NewGameStateFactory(repository, config).Create(["hero"], round: 4);

        var skill = Assert.Single(state.Party.GetMember("hero").ExternalSkills);
        Assert.Equal(12, skill.Level);
    }

    [Fact]
    public void PartyServiceJoin_KeepsDefinitionSkillLevelsWhenConfiguredNotToMaximizeNewPartyCharacterSkills()
    {
        var slash = TestContentFactory.CreateExternalSkill("slash");
        var breath = TestContentFactory.CreateInternalSkill("breath");
        var allyDefinition = TestContentFactory.CreateCharacterDefinition(
            "ally",
            externalSkills: [new InitialExternalSkillEntryDefinition(slash, Level: 3)],
            internalSkills: [new InitialInternalSkillEntryDefinition(breath, Level: 4)]);
        var repository = TestContentFactory.CreateRepository(
            characters: [allyDefinition],
            externalSkills: [slash],
            internalSkills: [breath]);
        var config = new GameConfig
        {
            MaximizeNewPartyCharacterSkills = false,
            BaseExternalSkillMaxLevel = 7,
            BaseInternalSkillMaxLevel = 9,
        };
        var session = new GameSession(new GameState(), repository, config: config);

        session.PartyService.Join("ally");

        var ally = Assert.Single(session.State.Party.Members);
        Assert.Equal("ally", ally.Id);
        Assert.Equal(3, Assert.Single(ally.ExternalSkills).Level);
        Assert.Equal(4, Assert.Single(ally.InternalSkills).Level);
    }
}
