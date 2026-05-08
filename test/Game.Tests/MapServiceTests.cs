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
    public void InteractWithLocation_LargeMap_AppliesDistanceCostAndMarksOnceEventCompleted()
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
        Assert.True(state.MapEventProgress.IsCompleted(WorldVillageEventKey));
        Assert.Null(result.EnterResult);
    }

    [Fact]
    public void InteractWithLocation_CompletedOnceEvent_DoesNotTriggerAgain()
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
        var result = session.MapService.InteractWithLocation(location);

        Assert.Equal(MapService.MapInteractionOutcome.Blocked, result.Outcome);
        Assert.Equal(0, result.ConsumedTimeSlots);
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
                        Type = "story",
                        TargetId = "story_old",
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
    public void EnterMap_SmallMap_NotHasTimeKeyCondition_StillUsesMissingItemSemantics()
    {
        var keyItem = new NormalItemDefinition
        {
            Id = "quest_token",
            Name = "quest_token",
            Type = ItemType.QuestItem,
        };
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
                        TargetId = "story_keeper",
                        Probability = 100,
                        Conditions =
                        [
                            new MapEventConditionDefinition
                            {
                                Type = "not_has_time_key",
                                Value = "quest_token",
                            },
                        ],
                    },
                ]));
        var repository = TestContentFactory.CreateRepository(maps: [conditionedMap], items: [keyItem]);

        var withoutItem = new GameSession(new GameState(), repository);
        var visibleLocations = withoutItem.MapService.EnterMap("inn").Locations;
        Assert.Single(visibleLocations);

        var stateWithItem = new GameState();
        stateWithItem.Inventory.AddItem(keyItem, 1);
        var withItem = new GameSession(stateWithItem, repository);
        var hiddenLocations = withItem.MapService.EnterMap("inn").Locations;
        Assert.Empty(hiddenLocations);
    }

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
