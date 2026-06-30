using Game.Core.Abstractions;
using Game.Core;
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

        _session.BattleService.RestorePartyBattleResources();

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
            PendingInteraction = _session.WorldTriggerService.ResolvePendingTrigger(),
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

        var movement = MoveHeroIfNeeded(location);
        var consumedTimeSlots = movement.ConsumedTimeSlots + 1;
        State.Clock.AdvanceTimeSlots(1);
        _session.Events.Publish(new ClockChangedEvent());

        MarkEventCompletedIfNeeded(
            location.Event,
            location.EventIndex,
            BuildLocationEventKey(location.MapId, location.Location.Id, location.EventIndex));

        return ResolveMapEvent(location.Event, consumedTimeSlots, movement.Result);
    }

    public int PreviewInteractionConsumedTimeSlots((string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex) location)
    {
        if (location.Event is null)
        {
            return 0;
        }

        return CalculateMoveConsumedTimeSlots(location.MapId, location.Location) + 1;
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

            if (Probability.RollPercentage(mapEvent.Probability))
            {
                return (mapEvent, mapEvent.RepeatMode == RepeatMode.Once ? index : -1);
            }
        }

        return null;
    }

    private (int ConsumedTimeSlots, MapMovementResult? Result) MoveHeroIfNeeded((string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex) location)
    {
        var consumedTimeSlots = CalculateMoveConsumedTimeSlots(location.MapId, location.Location);
        if (consumedTimeSlots > 0)
        {
            State.Clock.AdvanceTimeSlots(consumedTimeSlots);
        }

        if (location.Location.Position is { } targetPosition &&
            ContentRepository.GetMap(location.MapId).Kind == MapKind.Large)
        {
            var currentPosition = State.Location.TryGetLargeMapPosition(location.MapId, out var position)
                ? position
                : MapPosition.Zero;
            State.Location.SetLargeMapPosition(location.MapId, targetPosition);
            var movement = currentPosition == targetPosition
                ? null
                : new MapMovementResult(location.MapId, currentPosition, targetPosition);
            return (consumedTimeSlots, movement);
        }

        return (consumedTimeSlots, null);
    }

    private int CalculateMoveConsumedTimeSlots(string mapId, MapLocationDefinition location)
    {
        if (location.Position is not { } targetPosition ||
            ContentRepository.GetMap(mapId).Kind != MapKind.Large)
        {
            return 0;
        }

        var currentPosition = State.Location.TryGetLargeMapPosition(mapId, out var position)
            ? position
            : MapPosition.Zero;
        return (int)(currentPosition.DistanceTo(targetPosition) / 10d);
    }

    private MapInteractionResult ChangeMapFromEvent(string targetMapId, int consumedTimeSlots, MapMovementResult? movement)
    {
        var enterResult = EnterMap(targetMapId);
        return new MapInteractionResult
        {
            Outcome = MapInteractionOutcome.MapChanged,
            TargetId = targetMapId,
            ConsumedTimeSlots = consumedTimeSlots + enterResult.ConsumedTimeSlots,
            EnterResult = enterResult,
            Movement = movement,
        };
    }

    private MapInteractionResult ResolveMapEvent(MapEventDefinition mapEvent, int consumedTimeSlots, MapMovementResult? movement) =>
        mapEvent.Type switch
        {
            "map" => ChangeMapFromEvent(mapEvent.TargetId, consumedTimeSlots, movement),
            "story" => BuildInteractionResult(MapInteractionOutcome.StoryRequested, mapEvent, consumedTimeSlots, movement),
            "shop" => BuildInteractionResult(MapInteractionOutcome.ShopRequested, mapEvent, consumedTimeSlots, movement),
            "xiangzi" => BuildInteractionResult(MapInteractionOutcome.ChestRequested, mapEvent, consumedTimeSlots, movement),
            "battle" => BuildInteractionResult(MapInteractionOutcome.BattleRequested, mapEvent, consumedTimeSlots, movement),
            _ => BuildInteractionResult(MapInteractionOutcome.PlaceholderInteraction, mapEvent, consumedTimeSlots, movement),
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
        int consumedTimeSlots,
        MapMovementResult? movement)
    {
        return new MapInteractionResult
        {
            Outcome = outcome,
            Message = mapEvent.Description,
            TargetId = mapEvent.TargetId,
            ConsumedTimeSlots = consumedTimeSlots,
            Movement = movement,
        };
    }

    private static string BuildLocationEventKey(string mapId, string locationId, int eventIndex) =>
        $"{mapId}|{locationId}|{eventIndex}";

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
    public MapMovementResult? Movement { get; init; }
    public string? Message { get; init; }
    public string? TargetId { get; init; }
}

public sealed record MapMovementResult(
    string MapId,
    MapPosition From,
    MapPosition To);
