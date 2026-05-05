using Game.Application;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Persistence;
using Game.Core.Story;

namespace Game.Tests;

public sealed class SaveGameServiceTests
{
    [Fact]
    public void SaveGameService_RoundTripsPartyAndCharacters()
    {
        var basicAttack = TestContentFactory.CreateExternalSkill("basic_attack");
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_knight",
            externalSkills: [new InitialExternalSkillEntryDefinition(basicAttack)]);

        var repository = TestContentFactory.CreateRepository(
            characters: [definition],
            externalSkills: [basicAttack]);

        var first = TestContentFactory.CreateCharacterInstance("char_001", definition);
        first.Name = "Knight Alpha";
        var second = TestContentFactory.CreateCharacterInstance("char_002", definition);
        second.Name = "Knight Beta";
        var follower = TestContentFactory.CreateCharacterInstance("char_003", definition);
        follower.Name = "Knight Gamma";
        var reserve = TestContentFactory.CreateCharacterInstance("char_004", definition);
        reserve.Name = "Knight Delta";

        var party = new Party();
        party.AddMember(first);
        party.AddMember(second);
        party.AddFollower(follower);
        party.AddReserve(reserve);

        var state = new GameState();
        state.SetParty(party);
        state.Currency.AddSilver(140);
        state.Currency.AddGold(6);
        state.Clock.AdvanceTimeSlots(9);
        state.Clock.AdvanceDays(31);
        state.Location.ChangeMap("sample_map");
        state.Location.SetLargeMapPosition("world", new MapPosition(512, 410));
        state.MapEventProgress.MarkCompleted("world|village|0");
        state.Story.SetVariable("tutorial_finished", ExprValue.FromBoolean(true));
        state.Story.MarkCompleted("新手村_南贤开场");
        state.Story.SetLastStory("新手村_南贤开场");
        state.Journal.Append(ClockState.Restore(new ClockRecord(1, 2, 3, TimeSlot.Chou)), "拜访南贤");
        var session = new GameSession(state, repository);
        var service = session.SaveGameService;

        var saveGame = service.CreateSave();
        service.LoadSave(saveGame);

        Assert.Equal(["char_001", "char_002"], session.State.Party.Members.Select(member => member.Id).ToArray());
        Assert.Equal(["char_003"], session.State.Party.Followers.Select(member => member.Id).ToArray());
        Assert.Equal(["char_004"], session.State.Party.Reserves.Select(member => member.Id).ToArray());
        Assert.Equal(140, session.State.Currency.Silver);
        Assert.Equal(6, session.State.Currency.Gold);
        Assert.Equal(1, session.State.Clock.Year);
        Assert.Equal(2, session.State.Clock.Month);
        Assert.Equal(3, session.State.Clock.Day);
        Assert.Equal(32, session.State.Clock.TotalDays);
        Assert.Equal(TimeSlot.Chou, session.State.Clock.TimeSlot);
        Assert.Equal("sample_map", session.State.Location.CurrentMapId);
        Assert.Equal(new MapPosition(512, 410), session.State.Location.GetLargeMapPosition("world"));
        Assert.True(session.State.MapEventProgress.IsCompleted("world|village|0"));
        Assert.True(session.State.Story.IsStoryCompleted("新手村_南贤开场"));
        Assert.Equal("新手村_南贤开场", session.State.Story.LastStoryId);
        Assert.True(session.State.Story.TryGetVariable("tutorial_finished", out var tutorialFinished));
        Assert.True(tutorialFinished.Boolean);
        var journalEntry = Assert.Single(session.State.Journal.Entries);
        Assert.Equal("拜访南贤", journalEntry.Text);
    }
}
