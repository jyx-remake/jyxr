using Game.Core.Abstractions;
using Game.Core;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Expressions;

namespace Game.Application;

public sealed class MapService
{
    private readonly GameSession _session;
    private readonly GameConditionExpressionService _conditions;

    public MapService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _conditions = new GameConditionExpressionService(session);
    }

    private GameState State => _session.State;
    private IContentRepository ContentRepository => _session.ContentRepository;

    public MapEnterResult EnterMap(string mapId) => EnterMapCore(mapId, null);

    public MapEnterResult EnterMap(string mapId, string locationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locationId);
        return EnterMapCore(mapId, locationId);
    }

    /// <summary>
    /// Records the remembered position on a large map without changing the
    /// currently displayed map.  Legacy story scripts use this to prepare a
    /// destination before entering a small scene.
    /// </summary>
    public void SetLocation(string mapId, string locationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);
        ArgumentException.ThrowIfNullOrWhiteSpace(locationId);

        var map = ContentRepository.GetMap(mapId);
        if (map.Kind != MapKind.Large)
        {
            throw new InvalidOperationException(
                $"Map '{map.Id}' is not a large map and cannot store a location.");
        }

        State.Location.SetLargeMapPosition(map.Id, ResolveLocationPosition(map, locationId));
    }

    private MapEnterResult EnterMapCore(string mapId, string? locationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);

        var map = ContentRepository.GetMap(mapId);
        MapPosition? currentPosition = null;
        if (locationId is not null)
        {
            if (map.Kind != MapKind.Large)
            {
                throw new InvalidOperationException(
                    $"Map '{map.Id}' is not a large map and cannot be entered at location '{locationId}'.");
            }

            currentPosition = ResolveLocationPosition(map, locationId);
        }
        else if (map.Kind == MapKind.Large)
        {
            currentPosition = State.Location.TryGetLargeMapPosition(map.Id, out var rememberedPosition)
                ? rememberedPosition
                : ResolveDefaultPosition(map);
        }

        _session.BattleService.RestorePartyBattleResources();
        State.Location.ChangeMap(map.Id);
        if (currentPosition is not null)
        {
            State.Location.SetLargeMapPosition(map.Id, currentPosition.Value);
        }

        _session.Events.Publish(new MapChangedEvent(map.Id));

        return new MapEnterResult
        {
            Map = map,
            HeroPosition = currentPosition,
            ConsumedTimeSlots = 0,
            PendingInteraction = _session.WorldTriggerService.ResolvePendingTrigger(),
            Locations = BuildLocations(map),
        };
    }

    private static MapPosition ResolveDefaultPosition(MapDefinition map)
    {
        if (string.IsNullOrWhiteSpace(map.DefaultLocation))
        {
            if (map.Locations.Count == 0)
            {
                return MapPosition.Zero;
            }

            throw new InvalidOperationException($"Large map '{map.Id}' does not define a default location.");
        }

        return ResolveLocationPosition(map, map.DefaultLocation);
    }

    private static MapPosition ResolveLocationPosition(MapDefinition map, string locationId)
    {
        var location = map.Locations.SingleOrDefault(candidate =>
            string.Equals(candidate.Id, locationId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Large map '{map.Id}' does not contain location '{locationId}'.");
        return location.Position
            ?? throw new InvalidOperationException(
                $"Large map '{map.Id}' location '{location.Id}' does not define a position.");
    }

    public MapInteractionResult InteractWithLocation((string MapId, MapLocationDefinition Location, MapEventDefinition? Event) location)
    {
        if (location.Event is null)
        {
            return new MapInteractionResult
            {
                Command = null,
            };
        }

        var movement = MoveHeroIfNeeded(location);
        var consumedTimeSlots = movement.ConsumedTimeSlots + 1;
        State.Clock.AdvanceTimeSlots(1);
        _session.Events.Publish(new ClockChangedEvent());

        return new MapInteractionResult
        {
            Command = location.Event.Action,
            Message = location.Event.Description,
            ConsumedTimeSlots = consumedTimeSlots,
            Movement = movement.Result,
            OriginatingState = State,
            MapEventOccurrenceKey = location.Event.RepeatMode == RepeatMode.Once
                ? new MapEventKey(location.MapId, location.Location.Id, location.Event.Id)
                : null,
        };
    }

    public int PreviewInteractionConsumedTimeSlots((string MapId, MapLocationDefinition Location, MapEventDefinition? Event) location)
    {
        if (location.Event is null)
        {
            return 0;
        }

        return CalculateMoveConsumedTimeSlots(location.MapId, location.Location) + 1;
    }

    private IReadOnlyList<(string MapId, MapLocationDefinition Location, MapEventDefinition? Event)> BuildLocations(MapDefinition map)
    {
        var locations = new List<(string MapId, MapLocationDefinition Location, MapEventDefinition? Event)>(map.Locations.Count);
        foreach (var location in map.Locations)
        {
            var mapEvent = FindTriggerEvent(map.Id, location);
            if (mapEvent is null &&
                (map.Kind == MapKind.Small ||
                 location.HideWhenNoEvent))
            {
                continue;
            }

            locations.Add((map.Id, location, mapEvent));
        }

        return locations;
    }

    private MapEventDefinition? FindTriggerEvent(string mapId, MapLocationDefinition location)
    {
        foreach (var mapEvent in location.Events)
        {
            if (HasReachedRepeatLimit(mapId, location.Id, mapEvent))
            {
                continue;
            }

            if (!_conditions.Evaluate(mapEvent.When))
            {
                continue;
            }

            return mapEvent;
        }

        return null;
    }

    private (int ConsumedTimeSlots, MapMovementResult? Result) MoveHeroIfNeeded((string MapId, MapLocationDefinition Location, MapEventDefinition? Event) location)
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
        var map = ContentRepository.GetMap(mapId);
        if (location.Position is not { } targetPosition || map.Kind != MapKind.Large)
        {
            return 0;
        }

        if (!double.IsFinite(map.TravelSpeed) || map.TravelSpeed <= 0d)
        {
            throw new InvalidOperationException($"Large map '{map.Id}' must have a positive travel speed.");
        }

        var currentPosition = State.Location.TryGetLargeMapPosition(mapId, out var position)
            ? position
            : MapPosition.Zero;
        return (int)Math.Floor(currentPosition.DistanceTo(targetPosition) / map.TravelSpeed);
    }

    public void CompleteInteraction(MapInteractionResult interaction)
    {
        ArgumentNullException.ThrowIfNull(interaction);
        if (ReferenceEquals(interaction.OriginatingState, State) &&
            interaction.MapEventOccurrenceKey is { } eventKey)
        {
            State.MapEventProgress.RecordOccurrence(eventKey.MapId, eventKey.LocationId, eventKey.EventId);
        }
    }

    private bool HasReachedRepeatLimit(
        string mapId,
        string locationId,
        MapEventDefinition mapEvent)
    {
        if (mapEvent.RepeatMode != RepeatMode.Once || mapEvent.RepeatLimit == -1)
        {
            return false;
        }

        var limit = mapEvent.RepeatLimit ?? 1;
        var occurrenceCount = TryGetRepeatedStoryId(mapEvent, out var storyId)
            ? State.Story.GetCompletionCount(storyId)
            : State.MapEventProgress.GetOccurrenceCount(mapId, locationId, mapEvent.Id);
        return occurrenceCount >= limit;
    }

    private static bool TryGetRepeatedStoryId(MapEventDefinition mapEvent, out string storyId)
    {
        storyId = string.Empty;
        if (!string.Equals(mapEvent.Action.Root.Name, "story", StringComparison.Ordinal) ||
            mapEvent.Action.Root.Arguments.Count != 1 ||
            mapEvent.Action.Root.Arguments[0] is not LiteralExpressionSyntax literal ||
            literal.Value.Kind != ExpressionValueKind.String)
        {
            return false;
        }

        storyId = literal.Value.AsString("map story repeat target");
        return !string.IsNullOrWhiteSpace(storyId);
    }

}

public sealed record MapEnterResult
{
    public required MapDefinition Map { get; init; }
    public required int ConsumedTimeSlots { get; init; }
    public MapInteractionResult? PendingInteraction { get; init; }
    public IReadOnlyList<(string MapId, MapLocationDefinition Location, MapEventDefinition? Event)> Locations { get; init; } = [];
    public MapPosition? HeroPosition { get; init; }
}

public sealed record MapInteractionResult
{
    public ParsedCall? Command { get; init; }
    public int ConsumedTimeSlots { get; init; }
    public MapMovementResult? Movement { get; init; }
    public string? Message { get; init; }
    internal GameState? OriginatingState { get; init; }
    internal MapEventKey? MapEventOccurrenceKey { get; init; }
}

public sealed record MapMovementResult(
    string MapId,
    MapPosition From,
    MapPosition To);
