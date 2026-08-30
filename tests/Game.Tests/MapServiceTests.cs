using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Expressions;

namespace Game.Tests;

public sealed class MapServiceTests
{
    private const string WorldVillageEventId = "intro";

    [Fact]
    public void EnterMap_LargeMap_FirstVisitUsesDefaultLocation()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("start", position: new MapPosition(12, 34)),
            CreateLocation("gate", position: new MapPosition(56, 78)));
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(maps: [worldMap]));

        var result = session.MapService.EnterMap("world");

        Assert.Equal(new MapPosition(12, 34), result.HeroPosition);
        Assert.Equal(new MapPosition(12, 34), session.State.Location.GetLargeMapPosition("world"));
    }

    [Fact]
    public void EnterMap_LargeMap_ExplicitLocationOverridesAndRemembersPosition()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("start", position: new MapPosition(12, 34)),
            CreateLocation(
                "hidden_gate",
                position: new MapPosition(56, 78),
                hideWhenNoEvent: true));
        var state = new GameState();
        state.Location.SetLargeMapPosition("world", new MapPosition(90, 100));
        var session = new GameSession(state, TestContentFactory.CreateRepository(maps: [worldMap]));

        var result = session.MapService.EnterMap("world", "hidden_gate");

        Assert.Equal(new MapPosition(56, 78), result.HeroPosition);
        Assert.Equal(new MapPosition(56, 78), state.Location.GetLargeMapPosition("world"));
        Assert.Equal(new MapPosition(56, 78), session.MapService.EnterMap("world").HeroPosition);
    }

    [Fact]
    public void SetLocation_RecordsLargeMapPositionWithoutChangingCurrentMap()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("start", position: new MapPosition(12, 34)),
            CreateLocation("island", position: new MapPosition(56, 78)));
        var state = new GameState();
        state.Location.ChangeMap("inn");
        var session = new GameSession(state, TestContentFactory.CreateRepository(maps: [worldMap]));

        session.MapService.SetLocation("world", "island");

        Assert.Equal("inn", state.Location.CurrentMapId);
        Assert.Equal(new MapPosition(56, 78), state.Location.GetLargeMapPosition("world"));
    }

    [Fact]
    public void EnterMap_MultipleLargeMapsRememberPositionsIndependently()
    {
        var world = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("start", position: new MapPosition(10, 20)),
            CreateLocation("gate", position: new MapPosition(30, 40)));
        var islands = CreateMap(
            "islands",
            MapKind.Large,
            CreateLocation("port", position: new MapPosition(100, 200)),
            CreateLocation("dock", position: new MapPosition(300, 400)));
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(maps: [world, islands]));

        session.MapService.EnterMap("world", "gate");
        session.MapService.EnterMap("islands", "dock");

        Assert.Equal(new MapPosition(30, 40), session.MapService.EnterMap("world").HeroPosition);
        Assert.Equal(new MapPosition(300, 400), session.MapService.EnterMap("islands").HeroPosition);
    }

    [Fact]
    public void EnterMap_InvalidExplicitLocationDoesNotChangeLocationState()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("start", position: new MapPosition(12, 34)));
        var state = new GameState();
        state.Location.ChangeMap("inn");
        state.Location.SetLargeMapPosition("world", new MapPosition(90, 100));
        var session = new GameSession(state, TestContentFactory.CreateRepository(maps: [worldMap]));

        Assert.Throws<InvalidOperationException>(() =>
            session.MapService.EnterMap("world", "missing"));

        Assert.Equal("inn", state.Location.CurrentMapId);
        Assert.Equal(new MapPosition(90, 100), state.Location.GetLargeMapPosition("world"));
    }

    [Fact]
    public void EnterMap_RejectsExplicitLocationForSmallMapWithoutChangingCurrentMap()
    {
        var inn = CreateMap("inn", MapKind.Small, CreateLocation("door"));
        var state = new GameState();
        state.Location.ChangeMap("world");
        var session = new GameSession(state, TestContentFactory.CreateRepository(maps: [inn]));

        Assert.Throws<InvalidOperationException>(() => session.MapService.EnterMap("inn", "door"));

        Assert.Equal("world", state.Location.CurrentMapId);
    }

    [Fact]
    public void InteractWithLocation_UsesMapTravelSpeedForPreviewAndMovement()
    {
        var target = CreateLocation(
            "village",
            position: new MapPosition(44, 0),
            events:
            [
                new MapEventDefinition
                {
                    Id = WorldVillageEventId,
                    Action = Call("story('story_intro')"),
                },
            ]);
        var worldMap = CreateMap("world", MapKind.Large, target) with { TravelSpeed = 22d };
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(maps: [worldMap]));
        var location = Assert.Single(session.MapService.EnterMap("world").Locations);
        session.State.Location.SetLargeMapPosition("world", MapPosition.Zero);

        Assert.Equal(3, session.MapService.PreviewInteractionConsumedTimeSlots(location));

        var result = session.MapService.InteractWithLocation(location);

        Assert.Equal(3, result.ConsumedTimeSlots);
        Assert.Equal(new MapPosition(44, 0), session.State.Location.GetLargeMapPosition("world"));
        Assert.Equal(TimeSlot.Wei, session.State.Clock.TimeSlot);
    }

    [Fact]
    public void EnterMap_LargeMap_UsesRememberedPositionWithoutConsumingTime()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("village", position: new MapPosition(10, 20)),
            CreateLocation("sect_gate", position: new MapPosition(40, 80)));
        var repository = TestContentFactory.CreateRepository(maps: [worldMap]);

        var state = new GameState();
        state.Location.SetLargeMapPosition("world", new MapPosition(512, 410));
        var session = new GameSession(state, repository);

        var result = session.MapService.EnterMap("world");

        Assert.Equal("world", state.Location.CurrentMapId);
        Assert.Equal(new MapPosition(512, 410), state.Location.GetLargeMapPosition("world"));
        Assert.Equal(0, result.ConsumedTimeSlots);
        Assert.Equal(TimeSlot.Chen, state.Clock.TimeSlot);
        Assert.Equal(new MapPosition(512, 410), result.HeroPosition);
    }

    [Fact]
    public void EnterMap_LargeMap_NoEventPresentationOnlyFiltersHiddenLocation()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("default"),
            CreateLocation("hidden", hideWhenNoEvent: true),
            CreateLocation(
                "custom",
                noEventImage: "custom.icon"));
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(maps: [worldMap]));

        var locations = session.MapService.EnterMap("world").Locations;

        Assert.Equal(["default", "custom"], locations.Select(location => location.Location.Id).ToArray());
        Assert.All(locations, location => Assert.Null(location.Event));
        Assert.Equal("custom.icon", locations[1].Location.NoEventImage);
    }

    [Fact]
    public void EnterMap_LargeMap_HidesLocationWhenNoEventConditionMatches()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation(
                "hidden",
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "world-hidden-intro",
                        Action = Call("story('story_intro')"),
                        When = Expr("false"),
                    },
                ],
                hideWhenNoEvent: true));
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(maps: [worldMap]));

        Assert.Empty(session.MapService.EnterMap("world").Locations);
    }

    [Fact]
    public void EnterMap_LargeMap_HidesLocationAfterOnceEventCompletes()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation(
                "village",
                events:
                [
                    new MapEventDefinition
                    {
                        Id = WorldVillageEventId,
                        Action = Call("story('story_intro')"),
                        RepeatMode = RepeatMode.Once,
                    },
                ],
                hideWhenNoEvent: true));
        var state = new GameState();
        state.MapEventProgress.MarkCompleted("world", "village", WorldVillageEventId);
        var session = new GameSession(state, TestContentFactory.CreateRepository(maps: [worldMap]));

        Assert.Empty(session.MapService.EnterMap("world").Locations);
    }

    [Fact]
    public void EnterMap_LargeMap_HideWhenNoEventDoesNotHideAvailableEvent()
    {
        var mapEvent = new MapEventDefinition
        {
            Id = WorldVillageEventId,
            Action = Call("story('story_intro')"),
            Image = "event.icon",
        };
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation(
                "village",
                events: [mapEvent],
                hideWhenNoEvent: true));
        var session = new GameSession(
            new GameState(),
            TestContentFactory.CreateRepository(maps: [worldMap]));

        var location = Assert.Single(session.MapService.EnterMap("world").Locations);

        Assert.Same(mapEvent, location.Event);
        Assert.Equal("event.icon", location.Event!.Image);
    }

    [Fact]
    public void EnterMap_WhenWorldTriggerConditionMatches_ReturnsPendingStoryInteraction()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("village", position: new MapPosition(10, 20)));
        var worldTrigger = new WorldTriggerDefinition
        {
            Id = "story_global",
            Action = Call("story('story_global')"),
            When = Expr("friend_count >= 4"),
        };
        var repository = TestContentFactory.CreateRepository(maps: [worldMap], worldTriggers: [worldTrigger]);

        var state = new GameState();
        state.Party.AddMember(CreateCharacter("hero"));
        state.Party.AddMember(CreateCharacter("ally_1"));
        state.Party.AddMember(CreateCharacter("ally_2"));
        state.Party.AddMember(CreateCharacter("ally_3"));
        var session = new GameSession(state, repository);

        var result = session.MapService.EnterMap("world");

        Assert.NotNull(result.PendingInteraction);
        Assert.Equal("story", result.PendingInteraction!.Command!.Root.Name);
        Assert.Equal("story_global", CallArgument(result.PendingInteraction.Command));
        Assert.True(state.WorldTriggers.IsCompleted("story_global"));
    }

    [Fact]
    public void EnterMap_WorldTriggerFriendCount_DoesNotCountFollowers()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("village", position: new MapPosition(10, 20)));
        var worldTrigger = new WorldTriggerDefinition
        {
            Id = "story_global",
            Action = Call("story('story_global')"),
            When = Expr("friend_count >= 4"),
        };
        var repository = TestContentFactory.CreateRepository(maps: [worldMap], worldTriggers: [worldTrigger]);

        var state = new GameState();
        state.Party.AddMember(CreateCharacter("hero"));
        state.Party.AddMember(CreateCharacter("ally_1"));
        state.Party.AddFollower(CreateCharacter("ally_2"));
        state.Party.AddFollower(CreateCharacter("ally_3"));
        var session = new GameSession(state, repository);

        var result = session.MapService.EnterMap("world");

        Assert.Null(result.PendingInteraction);
    }

    [Fact]
    public void InteractWithLocation_LargeMap_OnceEventCompletesOnlyAfterSuccessfulDispatch()
    {
        var villageEvent = new MapEventDefinition
        {
            Id = WorldVillageEventId,
            Action = Call("story('story_intro')"),
            RepeatMode = RepeatMode.Once,
            Description = "村口奇遇",
        };
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("village", position: new MapPosition(30, 40), events: [villageEvent]));
        var repository = TestContentFactory.CreateRepository(maps: [worldMap]);

        var state = new GameState();
        state.Location.SetLargeMapPosition("world", new MapPosition(0, 0));
        var session = new GameSession(state, repository);

        var location = session.MapService.EnterMap("world")
            .Locations
            .Single();
        var result = session.MapService.InteractWithLocation(location);

        Assert.Equal("story", result.Command!.Root.Name);
        Assert.Equal(6, result.ConsumedTimeSlots);
        Assert.Equal(TimeSlot.Xu, state.Clock.TimeSlot);
        Assert.Equal(new MapPosition(30, 40), state.Location.GetLargeMapPosition("world"));
        Assert.False(state.MapEventProgress.IsCompleted("world", "village", WorldVillageEventId));
        session.MapService.CompleteInteraction(result);
        Assert.True(state.MapEventProgress.IsCompleted("world", "village", WorldVillageEventId));
    }

    [Fact]
    public void CompleteInteraction_DoesNotWriteOnceEventIntoReplacementState()
    {
        var villageEvent = new MapEventDefinition
        {
            Id = WorldVillageEventId,
            Action = Call("story('story_intro')"),
            RepeatMode = RepeatMode.Once,
        };
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("village", position: new MapPosition(30, 40), events: [villageEvent]));
        var originalState = new GameState();
        var session = new GameSession(
            originalState,
            TestContentFactory.CreateRepository(maps: [worldMap]));
        var location = session.MapService.EnterMap("world").Locations.Single();
        var interaction = session.MapService.InteractWithLocation(location);
        var replacementState = new GameState();

        session.ReplaceState(replacementState);
        session.MapService.CompleteInteraction(interaction);

        Assert.False(originalState.MapEventProgress.IsCompleted("world", "village", WorldVillageEventId));
        Assert.False(replacementState.MapEventProgress.IsCompleted("world", "village", WorldVillageEventId));
    }

    [Fact]
    public void EnterMap_StoryCompletionDoesNotReplaceMapEventIdentity()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation(
                "village",
                position: new MapPosition(30, 40),
                events:
                [
                    new MapEventDefinition
                    {
                        Id = WorldVillageEventId,
                        Action = Call("story('story_intro')"),
                        RepeatMode = RepeatMode.Once,
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [worldMap]);

        var state = new GameState();
        state.Story.MarkCompleted("story_intro");
        var session = new GameSession(state, repository);

        var enterResult = session.MapService.EnterMap("world");

        Assert.NotNull(enterResult.Locations.Single().Event);
    }

    [Fact]
    public void EnterMap_OnceEvent_WhenMapEventProgressCompleted_DoesNotTrigger()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation(
                "village",
                position: new MapPosition(30, 40),
                events:
                [
                    new MapEventDefinition
                    {
                        Id = WorldVillageEventId,
                        Action = Call("story('story_intro')"),
                        RepeatMode = RepeatMode.Once,
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [worldMap]);

        var state = new GameState();
        state.MapEventProgress.MarkCompleted("world", "village", WorldVillageEventId);
        var session = new GameSession(state, repository);

        var location = session.MapService.EnterMap("world").Locations.Single();

        Assert.Null(location.Event);
    }

    [Fact]
    public void EnterMap_TracksMatchingLocalEventIdsIndependentlyByLocation()
    {
        var map = CreateMap(
            "town",
            MapKind.Small,
            CreateLocation(
                "smith",
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "intro",
                        Action = Call("story('smith_intro')"),
                        RepeatMode = RepeatMode.Once,
                    },
                ]),
            CreateLocation(
                "inn",
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "intro",
                        Action = Call("story('inn_intro')"),
                        RepeatMode = RepeatMode.Once,
                    },
                ]));
        var state = new GameState();
        state.MapEventProgress.MarkCompleted("town", "smith", "intro");
        var session = new GameSession(state, TestContentFactory.CreateRepository(maps: [map]));

        var location = Assert.Single(session.MapService.EnterMap("town").Locations);

        Assert.Equal("inn", location.Location.Id);
        Assert.Equal("intro", location.Event!.Id);
    }

    [Fact]
    public void InteractWithLocation_NonStoryOnceEvent_UsesMapEventProgress()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation(
                "village",
                position: new MapPosition(30, 40),
                events:
                [
                    new MapEventDefinition
                    {
                        Id = WorldVillageEventId,
                        Action = Call("map('inn')"),
                        RepeatMode = RepeatMode.Once,
                    },
                ]));
        var innMap = CreateMap(
            "inn",
            MapKind.Small,
            CreateLocation("keeper"));
        var repository = TestContentFactory.CreateRepository(maps: [worldMap, innMap]);

        var state = new GameState();
        var session = new GameSession(state, repository);

        var location = session.MapService.EnterMap("world").Locations.Single();
        var result = session.MapService.InteractWithLocation(location);

        Assert.Equal("map", result.Command!.Root.Name);
        Assert.Equal("world", state.Location.CurrentMapId);
        Assert.False(state.MapEventProgress.IsCompleted("world", "village", WorldVillageEventId));
        session.MapService.CompleteInteraction(result);
        Assert.True(state.MapEventProgress.IsCompleted("world", "village", WorldVillageEventId));
        Assert.Null(session.MapService.EnterMap("world").Locations.Single().Event);
    }

    [Fact]
    public async Task InteractWithLocation_BusinessCommand_UsesStoryCommandRegistry()
    {
        var map = CreateMap(
            "inn",
            MapKind.Small,
            CreateLocation(
                "keeper",
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "inn-keeper-silver",
                        Action = Call("change_silver(25)"),
                        RepeatMode = RepeatMode.Once,
                    },
                ]));
        var session = new GameSession(new GameState(), TestContentFactory.CreateRepository(maps: [map]));

        var location = session.MapService.EnterMap("inn").Locations.Single();
        var interaction = session.MapService.InteractWithLocation(location);

        await session.StoryService.CommandDispatcher.ExecuteCallAsync(interaction.Command!);
        session.MapService.CompleteInteraction(interaction);

        Assert.Equal(25, session.State.Currency.Silver);
        Assert.Null(session.MapService.EnterMap("inn").Locations.SingleOrDefault().Event);
    }

    [Fact]
    public void InteractWithLocation_MapCommandIncludesMoveAndInteractionCostWithoutExecutingCommand()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation(
                "village",
                position: new MapPosition(30, 40),
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "world-village-enter_inn",
                        Action = Call("map('inn')"),
                    },
                ]));
        var innMap = CreateMap(
            "inn",
            MapKind.Small,
            CreateLocation("keeper"));
        var repository = TestContentFactory.CreateRepository(maps: [worldMap, innMap]);

        var state = new GameState();
        state.Location.SetLargeMapPosition("world", new MapPosition(0, 0));
        var session = new GameSession(state, repository);

        var location = session.MapService.EnterMap("world")
            .Locations
            .Single();
        var result = session.MapService.InteractWithLocation(location);

        Assert.Equal("map", result.Command!.Root.Name);
        Assert.Equal("inn", CallArgument(result.Command));
        Assert.Equal("world", state.Location.CurrentMapId);
        Assert.Equal(6, result.ConsumedTimeSlots);
        Assert.Equal(TimeSlot.Xu, state.Clock.TimeSlot);
    }

    [Fact]
    public void EnterMap_SmallMap_ReturnsTriggerableLocations()
    {
        var map = CreateMap(
            "inn",
            MapKind.Small,
            CreateLocation(
                "keeper",
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "inn-keeper-story",
                        Action = Call("story('story_keeper')"),
                        RepeatMode = RepeatMode.Once,
                        Description = "掌柜似有话说",
                    },
                ]),
            CreateLocation("door"));
        var repository = TestContentFactory.CreateRepository(maps: [map]);

        var state = new GameState();
        var session = new GameSession(state, repository);

        var result = session.MapService.EnterMap("inn");

        Assert.Equal("inn", result.Map.Id);
        Assert.Equal(MapKind.Small, result.Map.Kind);
        Assert.Null(result.HeroPosition);
        Assert.Equal(["keeper"], result.Locations.Select(location => location.Location.Id).ToArray());
        Assert.NotNull(result.Locations[0].Event);
        Assert.Equal("inn-keeper-story", result.Locations[0].Event!.Id);
        Assert.Equal("掌柜似有话说", result.Locations[0].Event!.Description);
    }

    [Fact]
    public void InteractWithLocation_SmallMap_ConsumesOneTimeSlot()
    {
        var map = CreateMap(
            "inn",
            MapKind.Small,
            CreateLocation(
                "keeper",
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "inn-keeper-story",
                        Action = Call("story('story_keeper')"),
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [map]);

        var state = new GameState();
        var session = new GameSession(state, repository);

        var location = session.MapService.EnterMap("inn").Locations.Single();
        var result = session.MapService.InteractWithLocation(location);

        Assert.Equal("story", result.Command!.Root.Name);
        Assert.Equal(1, result.ConsumedTimeSlots);
        Assert.Equal(TimeSlot.Si, state.Clock.TimeSlot);
    }

    [Fact]
    public void CompletedFirstEvent_DisplayAndInteractionBothSelectNextCurrentEvent()
    {
        var map = CreateMap(
            "inn",
            MapKind.Small,
            CreateLocation(
                "keeper",
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "inn-keeper-old_shop",
                        Action = Call("shop('shop_old')"),
                        RepeatMode = RepeatMode.Once,
                        Description = "旧事件",
                    },
                    new MapEventDefinition
                    {
                        Id = "inn-keeper-new_shop",
                        Action = Call("shop('shop_new')"),
                        Description = "新事件",
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [map]);

        var state = new GameState();
        state.MapEventProgress.MarkCompleted("inn", "keeper", "inn-keeper-old_shop");
        var session = new GameSession(state, repository);

        var enterResult = session.MapService.EnterMap("inn");
        var interactionResult = session.MapService.InteractWithLocation(enterResult.Locations.Single());

        Assert.NotNull(enterResult.Locations[0].Event);
        Assert.Equal("新事件", enterResult.Locations[0].Event!.Description);
        Assert.Equal("shop", interactionResult.Command!.Root.Name);
        Assert.Equal("shop_new", CallArgument(interactionResult.Command));
    }

    [Fact]
    public void EnterMap_SmallMap_TimeKeyConditionsUseActiveTimeKeySemantics()
    {
        var conditionedMap = CreateMap(
            "inn",
            MapKind.Small,
            CreateLocation(
                "keeper",
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "inn-keeper-without_key",
                        Action = Call("story('story_without_key')"),
                        When = Expr("!has_time_key('quest_cooldown')"),
                    },
                ]));
        var hasKeyMap = CreateMap(
            "inn_with_key",
            MapKind.Small,
            CreateLocation(
                "keeper",
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "inn_with_key-keeper-with_key",
                        Action = Call("story('story_with_key')"),
                        When = Expr("has_time_key('quest_cooldown')"),
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [conditionedMap, hasKeyMap]);

        var withoutTimeKey = new GameSession(new GameState(), repository);
        var visibleLocations = withoutTimeKey.MapService.EnterMap("inn").Locations;
        Assert.Single(visibleLocations);
        Assert.Equal("story_without_key", ActionId(visibleLocations[0].Event!));

        var stateWithTimeKey = new GameState();
        stateWithTimeKey.Story.SetTimeKey("quest_cooldown", stateWithTimeKey.Clock, 30);
        var withTimeKey = new GameSession(stateWithTimeKey, repository);
        Assert.Empty(withTimeKey.MapService.EnterMap("inn").Locations);

        var hasKeyLocations = withTimeKey.MapService.EnterMap("inn_with_key").Locations;
        Assert.Single(hasKeyLocations);
        Assert.Equal("story_with_key", ActionId(hasKeyLocations[0].Event!));
    }

    [Fact]
    public void EnterMap_DuplicatedStoryActions_UseMapProgressForOnceAndStoryStateForWhen()
    {
        var map = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation(
                "taihu",
                position: new MapPosition(30, 40),
                events:
                [
                    new MapEventDefinition
                    {
                        Id = "world-taihu-entry",
                        Action = Call("story('tlbb.dy_阿朱阿碧')"),
                        RepeatMode = RepeatMode.Once,
                        Description = "无条件入口",
                    },
                    new MapEventDefinition
                    {
                        Id = "world-taihu-before_finish",
                        Action = Call("story('tlbb.dy_阿朱阿碧')"),
                        Description = "结束前入口",
                        When = Expr("!story_completed('tlbb.dy_阿朱阿碧事件结束')"),
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [map]);

        var initialSession = new GameSession(new GameState(), repository);
        var initialLocation = initialSession.MapService.EnterMap("world").Locations.Single();
        Assert.Equal("无条件入口", initialLocation.Event!.Description);

        var entryCompletedState = new GameState();
        entryCompletedState.Story.MarkCompleted("tlbb.dy_阿朱阿碧");
        entryCompletedState.MapEventProgress.MarkCompleted("world", "taihu", "world-taihu-entry");
        var entryCompletedSession = new GameSession(entryCompletedState, repository);
        var entryCompletedLocation = entryCompletedSession.MapService.EnterMap("world").Locations.Single();
        Assert.Equal("结束前入口", entryCompletedLocation.Event!.Description);

        var finishedState = new GameState();
        finishedState.MapEventProgress.MarkCompleted("world", "taihu", "world-taihu-entry");
        finishedState.Story.MarkCompleted("tlbb.dy_阿朱阿碧");
        finishedState.Story.MarkCompleted("tlbb.dy_阿朱阿碧事件结束");
        var finishedSession = new GameSession(finishedState, repository);
        var finishedLocation = finishedSession.MapService.EnterMap("world").Locations.Single();
        Assert.Null(finishedLocation.Event);
    }

    private static ParsedCall Call(string source) => new ExpressionParser().ParseCall(source, "map test");
    private static ParsedExpression Expr(string source) => new ExpressionParser().ParseExpression(source, "map test");
    private static string CallArgument(ParsedCall call) =>
        ((LiteralExpressionSyntax)call.Root.Arguments.Single()).Value.AsString("map test call argument");
    private static string ActionId(MapEventDefinition mapEvent) =>
        CallArgument(mapEvent.Action);

    private static MapDefinition CreateMap(string id, MapKind kind, params MapLocationDefinition[] locations) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = kind,
            TravelSpeed = kind == MapKind.Large ? 10d : 0d,
            DefaultLocation = kind == MapKind.Large ? locations[0].Id : null,
            Locations = kind == MapKind.Large
                ? locations.Select(location => location.Position is null
                    ? location with { Position = MapPosition.Zero }
                    : location).ToArray()
                : locations,
        };

    private static MapLocationDefinition CreateLocation(
        string id,
        MapPosition? position = null,
        string? description = null,
        IReadOnlyList<MapEventDefinition>? events = null,
        bool hideWhenNoEvent = false,
        string? noEventImage = null) =>
        new()
        {
            Id = id,
            Name = id,
            Position = position,
            Description = description,
            HideWhenNoEvent = hideWhenNoEvent,
            NoEventImage = noEventImage,
            Events = events ?? [],
        };

    private static CharacterInstance CreateCharacter(string id)
    {
        var definition = TestContentFactory.CreateCharacterDefinition(id);
        return TestContentFactory.CreateCharacterInstance(id, definition);
    }
}
