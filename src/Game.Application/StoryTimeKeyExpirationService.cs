using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Story;

namespace Game.Application;

public sealed class StoryTimeKeyExpirationService
{
    private readonly GameSession _session;

    public StoryTimeKeyExpirationService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public IReadOnlyList<ExpiredStoryTimeKey> CheckExpired()
    {
        var clock = _session.State.Clock;
        var expired = _session.State.Story.TimeKeys.Values
            .Where(timeKey => IsAfter(clock, timeKey.DeadlineAt))
            .OrderBy(static timeKey => GetTotalTimeSlots(timeKey.DeadlineAt))
            .ThenBy(static timeKey => timeKey.Key, StringComparer.Ordinal)
            .ToArray();
        if (expired.Length == 0)
        {
            return [];
        }

        var results = new List<ExpiredStoryTimeKey>(expired.Length);
        foreach (var timeKey in expired)
        {
            if (!string.IsNullOrWhiteSpace(timeKey.TargetStoryId))
            {
                results.Add(new ExpiredStoryTimeKey(_session.State, timeKey));
            }
            else
            {
                _session.State.Story.RemoveTimeKey(timeKey.Key);
            }
        }

        if (results.Count != expired.Length) _session.Events.Publish(new StoryStateChangedEvent());
        return results;
    }

    public async Task<bool> ExecuteAsync(ExpiredStoryTimeKey expired, CancellationToken cancellationToken = default)
    {
        if (!ReferenceEquals(expired.State, _session.State) ||
            !_session.State.Story.TimeKeys.TryGetValue(expired.TimeKey.Key, out var current) ||
            !ReferenceEquals(current, expired.TimeKey))
        {
            return false;
        }

        using var execution = _session.Execution.Begin($"timed story '{current.TargetStoryId}'", cancellationToken);
        _session.State.Story.RemoveTimeKey(current.Key);
        _session.Events.Publish(new StoryStateChangedEvent());
        var outcome = await _session.StoryService.ExecuteAsync(current.TargetStoryId, cancellationToken: execution.Token);
        execution.Complete();
        return outcome == StoryOutcome.Completed &&
            _session.MapService.ReturnToMap(expired.State, $"timed story '{current.TargetStoryId}' completed");
    }

    private static bool IsAfter(ClockState current, ClockRecord deadline) =>
        GetTotalTimeSlots(current) > GetTotalTimeSlots(deadline);

    private static int GetTotalTimeSlots(ClockState clock) =>
        checked(clock.TotalDays * ClockState.TimeSlotsPerDay + (int)clock.TimeSlot);

    private static int GetTotalTimeSlots(ClockRecord clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var totalDays = checked(
            (clock.Year - 1) * ClockState.DaysPerYear +
            (clock.Month - 1) * ClockState.DaysPerMonth +
            clock.Day -
            1);
        return checked(totalDays * ClockState.TimeSlotsPerDay + (int)clock.TimeSlot);
    }
}

public sealed record ExpiredStoryTimeKey(GameState State, StoryTimeKeyState TimeKey);
