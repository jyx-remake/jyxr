using Game.Application;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Tests;

public sealed class PartyKickRecallTests
{
    [Fact]
    public void Kick_MovesMemberToReservesWithKickedState()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("ally");
        var state = new Game.Core.Model.GameState();
        var ally = TestContentFactory.CreateCharacterInstance("ally", definition, state.EquipmentInstanceFactory);
        state.Party.AddMember(ally);
        var session = new GameSession(state, TestContentFactory.CreateRepository(characters: [definition]));

        session.PartyService.Kick("ally");

        Assert.Empty(state.Party.Members);
        Assert.Same(ally, Assert.Single(state.Party.Reserves));
        Assert.Equal(CharacterLeaveState.Kicked, ally.LeaveState);
    }

    [Fact]
    public void Kick_RejectsHeroAndIgnoresNonMembers()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var state = new Game.Core.Model.GameState();
        var hero = TestContentFactory.CreateCharacterInstance(Game.Core.Model.Party.HeroCharacterId, definition, state.EquipmentInstanceFactory);
        state.Party.AddMember(hero);
        var session = new GameSession(state, TestContentFactory.CreateRepository(characters: [definition]));

        Assert.Throws<InvalidOperationException>(() => session.PartyService.Kick(Game.Core.Model.Party.HeroCharacterId));
        session.PartyService.Kick("stranger");

        Assert.Same(hero, Assert.Single(state.Party.Members));
    }

    [Fact]
    public void RecallKicked_ReturnsKickedCompanionToMembers()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("ally");
        var state = new Game.Core.Model.GameState();
        var ally = TestContentFactory.CreateCharacterInstance("ally", definition, state.EquipmentInstanceFactory);
        state.Party.AddMember(ally);
        var session = new GameSession(state, TestContentFactory.CreateRepository(characters: [definition]));
        session.PartyService.Kick("ally");

        session.PartyService.RecallKicked("ally");

        Assert.Same(ally, Assert.Single(state.Party.Members));
        Assert.Empty(state.Party.Reserves);
        Assert.Equal(CharacterLeaveState.None, ally.LeaveState);
    }

    [Fact]
    public void PlainJoin_CannotBringBackKickedCompanion()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("ally");
        var state = new Game.Core.Model.GameState();
        var ally = TestContentFactory.CreateCharacterInstance("ally", definition, state.EquipmentInstanceFactory);
        state.Party.AddMember(ally);
        var session = new GameSession(state, TestContentFactory.CreateRepository(characters: [definition]));
        session.PartyService.Kick("ally");

        session.PartyService.Join("ally");

        Assert.Empty(state.Party.Members);
        Assert.Same(ally, Assert.Single(state.Party.Reserves));
    }

    [Fact]
    public void RecallKicked_IgnoresCharactersThatWereNotKicked()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("ally");
        var state = new Game.Core.Model.GameState();
        var ally = TestContentFactory.CreateCharacterInstance("ally", definition, state.EquipmentInstanceFactory);
        state.Party.AddMember(ally);
        var session = new GameSession(state, TestContentFactory.CreateRepository(characters: [definition]));
        session.PartyService.LeaveTemp("ally");

        session.PartyService.RecallKicked("ally");

        Assert.Empty(state.Party.Members);
        Assert.Equal(CharacterLeaveState.Temp, ally.LeaveState);
    }
}
