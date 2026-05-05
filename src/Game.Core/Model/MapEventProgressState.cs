using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class MapEventProgressState
{
    private readonly HashSet<string> _completedEventIds = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> CompletedEventIds => _completedEventIds;

    public static MapEventProgressState Restore(MapEventProgressRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var state = new MapEventProgressState();
        foreach (var eventId in record.CompletedEventIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
            state._completedEventIds.Add(eventId);
        }

        return state;
    }

    public bool IsCompleted(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        return _completedEventIds.Contains(eventId);
    }

    public void MarkCompleted(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        _completedEventIds.Add(eventId);
    }

    public MapEventProgressRecord ToRecord() =>
        new(_completedEventIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray());
}
