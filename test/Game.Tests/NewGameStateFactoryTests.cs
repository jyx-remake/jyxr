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
            MaxExternalSkillLevel = 7,
            MaxInternalSkillLevel = 9,
        };

        var state = new NewGameStateFactory(repository, config).Create(["hero", "ally"]);

        Assert.Equal(["hero", "ally"], state.Party.Members.Select(member => member.Id).ToArray());
        var hero = state.Party.GetMember("hero");
        var externalSkill = Assert.Single(hero.ExternalSkills);
        var internalSkill = Assert.Single(hero.InternalSkills);
        Assert.Equal(7, externalSkill.MaxLevel);
        Assert.Equal(7, externalSkill.Level);
        Assert.Equal(9, internalSkill.MaxLevel);
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
            MaxExternalSkillLevel = 7,
            MaxInternalSkillLevel = 9,
        };

        var state = new NewGameStateFactory(repository, config).Create(["hero"]);

        var hero = state.Party.GetMember("hero");
        var externalSkill = Assert.Single(hero.ExternalSkills);
        var internalSkill = Assert.Single(hero.InternalSkills);
        Assert.Equal(7, externalSkill.MaxLevel);
        Assert.Equal(3, externalSkill.Level);
        Assert.Equal(9, internalSkill.MaxLevel);
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
            MaxExternalSkillLevel = 7,
            MaxInternalSkillLevel = 9,
        };
        var session = new GameSession(new GameState(), repository, config: config);

        session.PartyService.Join("ally");

        var ally = Assert.Single(session.State.Party.Members);
        Assert.Equal("ally", ally.Id);
        Assert.Equal(7, Assert.Single(ally.ExternalSkills).Level);
        Assert.Equal(9, Assert.Single(ally.InternalSkills).Level);
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
            MaxExternalSkillLevel = 7,
            MaxInternalSkillLevel = 9,
        };
        var session = new GameSession(new GameState(), repository, config: config);

        session.PartyService.Join("ally");

        var ally = Assert.Single(session.State.Party.Members);
        Assert.Equal("ally", ally.Id);
        Assert.Equal(3, Assert.Single(ally.ExternalSkills).Level);
        Assert.Equal(4, Assert.Single(ally.InternalSkills).Level);
    }
}
