namespace Game.Application;

/// <summary>Coalesces snapshots and acknowledges them only after a successful write.</summary>
public sealed class DeferredPersistence<T>(T initial, Action<T> write, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private T _saved = initial;
    private T _current = initial;
    private long? _scheduledAt;
    public bool IsDirty => !EqualityComparer<T>.Default.Equals(_saved, _current);
    public bool IsDue => _scheduledAt is { } timestamp && _clock.GetElapsedTime(timestamp) >= TimeSpan.FromSeconds(1);
    public void Update(T snapshot)
    {
        _current = snapshot;
        if (!IsDirty) _scheduledAt = null;
        else _scheduledAt ??= _clock.GetTimestamp();
    }
    public void FlushIfDue()
    {
        if (IsDue) Flush();
    }
    public void Flush()
    {
        if (!IsDirty) return;
        var snapshot = _current;
        // Failed writes remain dirty; retries are limited to once per second.
        _scheduledAt = _clock.GetTimestamp();
        write(snapshot);
        _saved = snapshot;
        if (!IsDirty) _scheduledAt = null;
    }
}
