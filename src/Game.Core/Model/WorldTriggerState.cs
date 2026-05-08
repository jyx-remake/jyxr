using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class WorldTriggerState
{
    private readonly HashSet<string> _completedTriggerIds = new(StringComparer.Ordinal);

    public bool IsBlocked { get; private set; }

    public IReadOnlyCollection<string> CompletedTriggerIds => _completedTriggerIds;

    public static WorldTriggerState Restore(WorldTriggerStateRecord? record)
    {
        var state = new WorldTriggerState();
        if (record is null)
        {
            return state;
        }

        state.IsBlocked = record.IsBlocked;
        foreach (var triggerId in record.CompletedTriggerIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(triggerId);
            state._completedTriggerIds.Add(triggerId);
        }

        return state;
    }

    public bool IsCompleted(string triggerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerId);
        return _completedTriggerIds.Contains(triggerId);
    }

    public void MarkCompleted(string triggerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerId);
        _completedTriggerIds.Add(triggerId);
    }

    public void Block() => IsBlocked = true;

    public void Unblock() => IsBlocked = false;

    public WorldTriggerStateRecord ToRecord() =>
        new(
            IsBlocked,
            _completedTriggerIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray());
}
