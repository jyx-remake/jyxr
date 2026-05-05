using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Application;

public sealed class MapService
{
    private readonly GameSession _session;
    private readonly MapConditionEvaluator _conditionEvaluator;

    public MapService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _conditionEvaluator = new MapConditionEvaluator(session);
    }

    private GameState State => _session.State;
    private IContentRepository ContentRepository => _session.ContentRepository;

    public enum MapInteractionOutcome
    {
        MapChanged,
        StoryRequested,
        ShopRequested,
        ChestRequested,
        BattleRequested,
        PlaceholderInteraction,
        Blocked,
    }

    public MapEnterResult EnterMap(string mapId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);

        var map = ContentRepository.GetMap(mapId);
        State.Location.ChangeMap(map.Id);
        _session.Events.Publish(new MapChangedEvent(map.Id));

        MapPosition? currentPosition = null;
        if (map.Kind == MapKind.Large)
        {
            currentPosition = State.Location.TryGetLargeMapPosition(map.Id, out var rememberedPosition)
                ? rememberedPosition
                : MapPosition.Zero;
            State.Location.SetLargeMapPosition(map.Id, currentPosition.Value);
        }

        return new MapEnterResult
        {
            Map = map,
            HeroPosition = currentPosition,
            ConsumedTimeSlots = 0,
            PendingInteraction = ResolveEnterEvent(map),
            Locations = BuildLocations(map),
        };
    }

    public MapInteractionResult InteractWithLocation((string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex) location)
    {
        if (location.Event is null)
        {
            return new MapInteractionResult
            {
                Outcome = MapInteractionOutcome.Blocked,
            };
        }

        var consumedTimeSlots = MoveHeroIfNeeded(location) + 1;
        State.Clock.AdvanceTimeSlots(1);
        _session.Events.Publish(new ClockChangedEvent());

        MarkEventCompletedIfNeeded(
            location.Event,
            location.EventIndex,
            BuildLocationEventKey(location.MapId, location.Location.Id, location.EventIndex));

        return ResolveMapEvent(location.Event, consumedTimeSlots);
    }

    private IReadOnlyList<(string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex)> BuildLocations(MapDefinition map)
    {
        var locations = new List<(string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex)>(map.Locations.Count);
        foreach (var location in map.Locations)
        {
            var mapEvent = FindTriggerEvent(map.Id, location);
            if (map.Kind == MapKind.Small && mapEvent is null)
            {
                continue;
            }

            locations.Add((map.Id, location, mapEvent?.Event, mapEvent?.Index ?? -1));
        }

        return locations;
    }

    private (MapEventDefinition Event, int Index)? FindTriggerEvent(string mapId, MapLocationDefinition location)
    {
        for (var index = 0; index < location.Events.Count; index++)
        {
            var mapEvent = location.Events[index];
            if (mapEvent.RepeatMode == RepeatMode.Once &&
                State.MapEventProgress.IsCompleted(BuildLocationEventKey(mapId, location.Id, index)))
            {
                continue;
            }

            if (!_conditionEvaluator.AreSatisfied(mapEvent.Conditions))
            {
                continue;
            }

            if (RollChance(mapEvent.Probability))
            {
                return (mapEvent, mapEvent.RepeatMode == RepeatMode.Once ? index : -1);
            }
        }

        return null;
    }

    private MapInteractionResult? ResolveEnterEvent(MapDefinition map)
    {
        for (var index = 0; index < map.EnterEvents.Count; index++)
        {
            var mapEvent = map.EnterEvents[index];
            if (mapEvent.RepeatMode == RepeatMode.Once &&
                State.MapEventProgress.IsCompleted(BuildEnterEventKey(map.Id, index)))
            {
                continue;
            }

            if (!_conditionEvaluator.AreSatisfied(mapEvent.Conditions))
            {
                continue;
            }

            if (!RollChance(mapEvent.Probability))
            {
                continue;
            }

            MarkEventCompletedIfNeeded(mapEvent, index, BuildEnterEventKey(map.Id, index));
            return ResolveMapEvent(mapEvent, 0);
        }

        return null;
    }

    private int MoveHeroIfNeeded((string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex) location)
    {
        if (location.Location.Position is not { } targetPosition ||
            ContentRepository.GetMap(location.MapId).Kind != MapKind.Large)
        {
            return 0;
        }

        var currentPosition = State.Location.TryGetLargeMapPosition(location.MapId, out var position)
            ? position
            : MapPosition.Zero;
        var consumedTimeSlots = (int)(currentPosition.DistanceTo(targetPosition) / 10d);
        if (consumedTimeSlots > 0)
        {
            State.Clock.AdvanceTimeSlots(consumedTimeSlots);
        }

        State.Location.SetLargeMapPosition(location.MapId, targetPosition);
        return consumedTimeSlots;
    }

    private MapInteractionResult ChangeMapFromEvent(string targetMapId, int consumedTimeSlots)
    {
        var enterResult = EnterMap(targetMapId);
        return new MapInteractionResult
        {
            Outcome = MapInteractionOutcome.MapChanged,
            TargetId = targetMapId,
            ConsumedTimeSlots = consumedTimeSlots + enterResult.ConsumedTimeSlots,
            EnterResult = enterResult,
        };
    }

    private MapInteractionResult ResolveMapEvent(MapEventDefinition mapEvent, int consumedTimeSlots) =>
        mapEvent.Type switch
        {
            "map" => ChangeMapFromEvent(mapEvent.TargetId, consumedTimeSlots),
            "story" => BuildInteractionResult(MapInteractionOutcome.StoryRequested, mapEvent, consumedTimeSlots),
            "shop" => BuildInteractionResult(MapInteractionOutcome.ShopRequested, mapEvent, consumedTimeSlots),
            "xiangzi" => BuildInteractionResult(MapInteractionOutcome.ChestRequested, mapEvent, consumedTimeSlots),
            "battle" => BuildInteractionResult(MapInteractionOutcome.BattleRequested, mapEvent, consumedTimeSlots),
            _ => BuildInteractionResult(MapInteractionOutcome.PlaceholderInteraction, mapEvent, consumedTimeSlots),
        };

    private void MarkEventCompletedIfNeeded(MapEventDefinition mapEvent, int eventIndex, string eventKey)
    {
        if (mapEvent.RepeatMode != RepeatMode.Once || eventIndex < 0)
        {
            return;
        }

        State.MapEventProgress.MarkCompleted(eventKey);
    }

    private static MapInteractionResult BuildInteractionResult(
        MapInteractionOutcome outcome,
        MapEventDefinition mapEvent,
        int consumedTimeSlots)
    {
        return new MapInteractionResult
        {
            Outcome = outcome,
            Message = mapEvent.Description,
            TargetId = mapEvent.TargetId,
            ConsumedTimeSlots = consumedTimeSlots,
        };
    }

    private static bool RollChance(int probability) =>
        Random.Shared.Next(100) < probability;

    private static string BuildLocationEventKey(string mapId, string locationId, int eventIndex) =>
        $"{mapId}|{locationId}|{eventIndex}";

    private static string BuildEnterEventKey(string mapId, int eventIndex) =>
        $"{mapId}|$enter|{eventIndex}";
}

public sealed record MapEnterResult
{
    public required MapDefinition Map { get; init; }
    public required int ConsumedTimeSlots { get; init; }
    public MapInteractionResult? PendingInteraction { get; init; }
    public IReadOnlyList<(string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex)> Locations { get; init; } = [];
    public MapPosition? HeroPosition { get; init; }
}

public sealed record MapInteractionResult
{
    public required MapService.MapInteractionOutcome Outcome { get; init; }
    public int ConsumedTimeSlots { get; init; }
    public MapEnterResult? EnterResult { get; init; }
    public string? Message { get; init; }
    public string? TargetId { get; init; }
}
