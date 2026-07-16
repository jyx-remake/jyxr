using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Tests;

public sealed class MapServiceTests
{
    private const string WorldVillageEventKey = "world|village|0";
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
    public void EnterMap_WhenWorldTriggerConditionMatches_ReturnsPendingStoryInteraction()
    {
        var worldMap = CreateMap(
            "world",
            MapKind.Large,
            CreateLocation("village", position: new MapPosition(10, 20)));
        var worldTrigger = new WorldTriggerDefinition
        {
            Id = "story_global",
            Type = "story",
            TargetId = "story_global",
            Probability = 100,
            Conditions =
            [
                new MapEventConditionDefinition
                {
                    Type = "friendCount",
                    Value = "4",
                },
            ],
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
        Assert.Equal(MapService.MapInteractionOutcome.StoryRequested, result.PendingInteraction!.Outcome);
        Assert.Equal("story_global", result.PendingInteraction.TargetId);
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
            Type = "story",
            TargetId = "story_global",
            Probability = 100,
            Conditions =
            [
                new MapEventConditionDefinition
                {
                    Type = "friendCount",
                    Value = "4",
                },
            ],
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
    public void InteractWithLocation_LargeMap_StoryOnceEventAppliesDistanceCostWithoutMarkingMapEventCompleted()
    {
        var villageEvent = new MapEventDefinition
        {
            Type = "story",
            TargetId = "story_intro",
            RepeatMode = RepeatMode.Once,
            Probability = 100,
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

        Assert.Equal(MapService.MapInteractionOutcome.StoryRequested, result.Outcome);
        Assert.Equal(6, result.ConsumedTimeSlots);
        Assert.Equal(TimeSlot.Xu, state.Clock.TimeSlot);
        Assert.Equal(new MapPosition(30, 40), state.Location.GetLargeMapPosition("world"));
        Assert.False(state.MapEventProgress.IsCompleted(WorldVillageEventKey));
        Assert.Null(result.EnterResult);
    }

    [Fact]
    public void EnterMap_StoryOnceEvent_WhenTargetStoryCompleted_DoesNotTrigger()
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
                        Type = "story",
                        TargetId = "story_intro",
                        RepeatMode = RepeatMode.Once,
                        Probability = 100,
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [worldMap]);

        var state = new GameState();
        state.Story.MarkCompleted("story_intro");
        var session = new GameSession(state, repository);

        var enterResult = session.MapService.EnterMap("world");

        Assert.Null(enterResult.Locations.Single().Event);
    }

    [Fact]
    public void EnterMap_StoryOnceEvent_WhenOnlyMapEventProgressCompleted_StillTriggers()
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
                        Type = "story",
                        TargetId = "story_intro",
                        RepeatMode = RepeatMode.Once,
                        Probability = 100,
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [worldMap]);

        var state = new GameState();
        state.MapEventProgress.MarkCompleted(WorldVillageEventKey);
        var session = new GameSession(state, repository);

        var location = session.MapService.EnterMap("world").Locations.Single();

        Assert.NotNull(location.Event);
        Assert.Equal("story_intro", location.Event!.TargetId);
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
                        Type = "map",
                        TargetId = "inn",
                        RepeatMode = RepeatMode.Once,
                        Probability = 100,
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

        Assert.Equal(MapService.MapInteractionOutcome.MapChanged, result.Outcome);
        Assert.True(state.MapEventProgress.IsCompleted(WorldVillageEventKey));
        Assert.Null(session.MapService.EnterMap("world").Locations.Single().Event);
    }

    [Fact]
    public void InteractWithLocation_MapEvent_ChangesMapAndIncludesMoveAndInteractionCost()
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
                        Type = "map",
                        TargetId = "inn",
                        Probability = 100,
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

        Assert.Equal(MapService.MapInteractionOutcome.MapChanged, result.Outcome);
        Assert.Equal("inn", state.Location.CurrentMapId);
        Assert.Equal(6, result.ConsumedTimeSlots);
        Assert.Equal(TimeSlot.Xu, state.Clock.TimeSlot);
        Assert.NotNull(result.EnterResult);
        Assert.Equal("inn", result.EnterResult!.Map.Id);
        Assert.Equal(MapKind.Small, result.EnterResult.Map.Kind);
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
                        Type = "story",
                        TargetId = "story_keeper",
                        RepeatMode = RepeatMode.Once,
                        Probability = 100,
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
        Assert.Equal(0, result.Locations[0].EventIndex);
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
                        Type = "story",
                        TargetId = "story_keeper",
                        Probability = 100,
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [map]);

        var state = new GameState();
        var session = new GameSession(state, repository);

        var location = session.MapService.EnterMap("inn").Locations.Single();
        var result = session.MapService.InteractWithLocation(location);

        Assert.Equal(MapService.MapInteractionOutcome.StoryRequested, result.Outcome);
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
                        Type = "shop",
                        TargetId = "shop_old",
                        RepeatMode = RepeatMode.Once,
                        Probability = 100,
                        Description = "旧事件",
                    },
                    new MapEventDefinition
                    {
                        Type = "shop",
                        TargetId = "shop_new",
                        Probability = 100,
                        Description = "新事件",
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [map]);

        var state = new GameState();
        state.MapEventProgress.MarkCompleted("inn|keeper|0");
        var session = new GameSession(state, repository);

        var enterResult = session.MapService.EnterMap("inn");
        var interactionResult = session.MapService.InteractWithLocation(enterResult.Locations.Single());

        Assert.NotNull(enterResult.Locations[0].Event);
        Assert.Equal("新事件", enterResult.Locations[0].Event!.Description);
        Assert.Equal(MapService.MapInteractionOutcome.ShopRequested, interactionResult.Outcome);
        Assert.Equal("shop_new", interactionResult.TargetId);
        Assert.Null(interactionResult.EnterResult);
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
                        Type = "story",
                        TargetId = "story_without_key",
                        Probability = 100,
                        Conditions = [CreateCondition("not_has_time_key", "quest_cooldown")],
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
                        Type = "story",
                        TargetId = "story_with_key",
                        Probability = 100,
                        Conditions = [CreateCondition("has_time_key", "quest_cooldown")],
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [conditionedMap, hasKeyMap]);

        var withoutTimeKey = new GameSession(new GameState(), repository);
        var visibleLocations = withoutTimeKey.MapService.EnterMap("inn").Locations;
        Assert.Single(visibleLocations);
        Assert.Equal("story_without_key", visibleLocations[0].Event!.TargetId);

        var stateWithTimeKey = new GameState();
        stateWithTimeKey.Story.SetTimeKey("quest_cooldown", stateWithTimeKey.Clock, 30);
        var withTimeKey = new GameSession(stateWithTimeKey, repository);
        Assert.Empty(withTimeKey.MapService.EnterMap("inn").Locations);

        var hasKeyLocations = withTimeKey.MapService.EnterMap("inn_with_key").Locations;
        Assert.Single(hasKeyLocations);
        Assert.Equal("story_with_key", hasKeyLocations[0].Event!.TargetId);
    }

    [Fact]
    public void EnterMap_LegacyDuplicatedStoryEvents_UseStoryCompletionForOnceAndConditions()
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
                        Type = "story",
                        TargetId = "tlbb.dy_阿朱阿碧",
                        RepeatMode = RepeatMode.Once,
                        Probability = 100,
                        Description = "无条件入口",
                    },
                    new MapEventDefinition
                    {
                        Type = "story",
                        TargetId = "tlbb.dy_阿朱阿碧",
                        Probability = 100,
                        Description = "结束前入口",
                        Conditions =
                        [
                            CreateCondition("should_not_finish", "tlbb.dy_阿朱阿碧事件结束"),
                        ],
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [map]);

        var initialSession = new GameSession(new GameState(), repository);
        var initialLocation = initialSession.MapService.EnterMap("world").Locations.Single();
        Assert.Equal("无条件入口", initialLocation.Event!.Description);

        var entryCompletedState = new GameState();
        entryCompletedState.Story.MarkCompleted("tlbb.dy_阿朱阿碧");
        entryCompletedState.MapEventProgress.MarkCompleted("world|taihu|0");
        var entryCompletedSession = new GameSession(entryCompletedState, repository);
        var entryCompletedLocation = entryCompletedSession.MapService.EnterMap("world").Locations.Single();
        Assert.Equal("结束前入口", entryCompletedLocation.Event!.Description);

        var finishedState = new GameState();
        finishedState.Story.MarkCompleted("tlbb.dy_阿朱阿碧");
        finishedState.Story.MarkCompleted("tlbb.dy_阿朱阿碧事件结束");
        var finishedSession = new GameSession(finishedState, repository);
        var finishedLocation = finishedSession.MapService.EnterMap("world").Locations.Single();
        Assert.Null(finishedLocation.Event);
    }

    private static MapEventConditionDefinition CreateCondition(string type, string value) =>
        new()
        {
            Type = type,
            Value = value,
        };

    private static MapDefinition CreateMap(string id, MapKind kind, params MapLocationDefinition[] locations) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = kind,
            Locations = locations,
        };

    private static MapLocationDefinition CreateLocation(
        string id,
        MapPosition? position = null,
        string? description = null,
        IReadOnlyList<MapEventDefinition>? events = null) =>
        new()
        {
            Id = id,
            Name = id,
            Position = position,
            Description = description,
            Events = events ?? [],
        };

    private static CharacterInstance CreateCharacter(string id)
    {
        var definition = TestContentFactory.CreateCharacterDefinition(id);
        return TestContentFactory.CreateCharacterInstance(id, definition);
    }
}
