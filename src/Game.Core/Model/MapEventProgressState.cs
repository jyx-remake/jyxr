using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class MapEventProgressState
{
    private readonly Dictionary<MapEventKey, int> _occurrences = [];

    public IReadOnlyCollection<MapEventKey> CompletedEvents => _occurrences.Keys;

    public static MapEventProgressState Restore(MapEventProgressRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var state = new MapEventProgressState();
        foreach (var completedEvent in record.CompletedEvents ?? [])
        {
            state._occurrences.TryAdd(CreateKey(
                completedEvent.MapId,
                completedEvent.LocationId,
                completedEvent.EventId), 1);
        }

        foreach (var occurrence in record.Occurrences ?? [])
        {
            if (occurrence.Count <= 0)
            {
                throw new InvalidDataException(
                    $"Map event occurrence count must be positive, but was {occurrence.Count}.");
            }

            state._occurrences[CreateKey(
                occurrence.MapId,
                occurrence.LocationId,
                occurrence.EventId)] = occurrence.Count;
        }

        return state;
    }

    public bool IsCompleted(string mapId, string locationId, string eventId)
    {
        return GetOccurrenceCount(mapId, locationId, eventId) > 0;
    }

    public void MarkCompleted(string mapId, string locationId, string eventId)
    {
        _occurrences.TryAdd(CreateKey(mapId, locationId, eventId), 1);
    }

    public int GetOccurrenceCount(string mapId, string locationId, string eventId) =>
        _occurrences.GetValueOrDefault(CreateKey(mapId, locationId, eventId));

    public int RecordOccurrence(string mapId, string locationId, string eventId)
    {
        var key = CreateKey(mapId, locationId, eventId);
        var current = _occurrences.GetValueOrDefault(key);
        var next = current == int.MaxValue ? int.MaxValue : current + 1;
        _occurrences[key] = next;
        return next;
    }

    public MapEventProgressRecord ToRecord() =>
        new(_occurrences.Keys
            .OrderBy(static key => key.MapId, StringComparer.Ordinal)
            .ThenBy(static key => key.LocationId, StringComparer.Ordinal)
            .ThenBy(static key => key.EventId, StringComparer.Ordinal)
            .Select(static key => new MapEventCompletionRecord(
                key.MapId,
                key.LocationId,
                key.EventId))
            .ToArray(),
            _occurrences
                .Where(static pair => pair.Value > 1)
                .OrderBy(static pair => pair.Key.MapId, StringComparer.Ordinal)
                .ThenBy(static pair => pair.Key.LocationId, StringComparer.Ordinal)
                .ThenBy(static pair => pair.Key.EventId, StringComparer.Ordinal)
                .Select(static pair => new MapEventOccurrenceRecord(
                    pair.Key.MapId,
                    pair.Key.LocationId,
                    pair.Key.EventId,
                    pair.Value))
                .ToArray());

    private static MapEventKey CreateKey(string mapId, string locationId, string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);
        ArgumentException.ThrowIfNullOrWhiteSpace(locationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        return new MapEventKey(mapId, locationId, eventId);
    }
}

public readonly record struct MapEventKey(string MapId, string LocationId, string EventId);
