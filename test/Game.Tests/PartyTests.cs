using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Tests;

public sealed class PartyTests
{
    [Fact]
    public void Party_AddsMembersInOrderAndResolvesByCharacterId()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var first = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var second = TestContentFactory.CreateCharacterInstance("char_002", definition);

        var party = new Party();
        party.AddMember(first);
        party.AddMember(second);

        Assert.Equal(["char_001", "char_002"], party.Members.Select(member => member.Id).ToArray());
        Assert.True(party.ContainsMember("char_002"));
        Assert.Same(second, party.GetMember("char_002"));
    }

    [Fact]
    public void Party_RejectsDuplicateMembers()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var party = new Party();
        party.AddMember(character);

        Assert.Throws<InvalidOperationException>(() => party.AddMember(character));
        Assert.Throws<InvalidOperationException>(() => party.AddFollower(character));
    }

    [Fact]
    public void Party_MovesCharactersBetweenMemberFollowerAndReservePools()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var party = new Party();
        party.AddMember(character);

        Assert.True(party.MoveToReserves(character.Id));
        Assert.Empty(party.Members);
        Assert.Same(character, Assert.Single(party.Reserves));
        Assert.True(party.ContainsReserve(character.Id));

        Assert.True(party.MoveToFollowers(character.Id));
        Assert.Empty(party.Reserves);
        Assert.Same(character, Assert.Single(party.Followers));

        Assert.True(party.MoveToMembers(character.Id));
        Assert.Same(character, Assert.Single(party.Members));
        Assert.Same(character, Assert.Single(party.GetAllCharacters()));
    }

    [Fact]
    public void Party_MoveMember_ReordersNonHeroMembers()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var hero = TestContentFactory.CreateCharacterInstance(Party.HeroCharacterId, definition);
        var second = TestContentFactory.CreateCharacterInstance("char_002", definition);
        var third = TestContentFactory.CreateCharacterInstance("char_003", definition);
        var party = new Party();
        party.AddMember(hero);
        party.AddMember(second);
        party.AddMember(third);

        var moved = party.MoveMember("char_002", 2);

        Assert.True(moved);
        Assert.Equal([Party.HeroCharacterId, "char_003", "char_002"], party.Members.Select(member => member.Id).ToArray());
    }

    [Fact]
    public void Party_MoveMember_RejectsDisplacingHeroFromFirstPosition()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var hero = TestContentFactory.CreateCharacterInstance(Party.HeroCharacterId, definition);
        var ally = TestContentFactory.CreateCharacterInstance("char_002", definition);
        var party = new Party();
        party.AddMember(hero);
        party.AddMember(ally);

        Assert.Throws<InvalidOperationException>(() => party.MoveMember("char_002", 0));
        Assert.Throws<InvalidOperationException>(() => party.MoveMember(Party.HeroCharacterId, 1));
    }

    [Fact]
    public void Party_GetNextMember_WrapsAroundByPartyOrder()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var hero = TestContentFactory.CreateCharacterInstance(Party.HeroCharacterId, definition);
        var ally = TestContentFactory.CreateCharacterInstance("char_002", definition);
        var third = TestContentFactory.CreateCharacterInstance("char_003", definition);
        var party = new Party();
        party.AddMember(hero);
        party.AddMember(ally);
        party.AddMember(third);

        Assert.Same(ally, party.GetNextMember(Party.HeroCharacterId));
        Assert.Same(hero, party.GetNextMember("char_003"));
    }

    [Fact]
    public void Party_GetPreviousMember_WrapsAroundByPartyOrder()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var hero = TestContentFactory.CreateCharacterInstance(Party.HeroCharacterId, definition);
        var ally = TestContentFactory.CreateCharacterInstance("char_002", definition);
        var third = TestContentFactory.CreateCharacterInstance("char_003", definition);
        var party = new Party();
        party.AddMember(hero);
        party.AddMember(ally);
        party.AddMember(third);

        Assert.Same(third, party.GetPreviousMember(Party.HeroCharacterId));
        Assert.Same(hero, party.GetPreviousMember("char_002"));
    }
}
