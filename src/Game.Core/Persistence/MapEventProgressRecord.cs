namespace Game.Core.Persistence;

public sealed record MapEventProgressRecord(
    IReadOnlyList<MapEventCompletionRecord>? CompletedEvents = null,
    IReadOnlyList<MapEventOccurrenceRecord>? Occurrences = null);

public sealed record MapEventCompletionRecord(
    string MapId,
    string LocationId,
    string EventId);

public sealed record MapEventOccurrenceRecord(
    string MapId,
    string LocationId,
    string EventId,
    int Count);
