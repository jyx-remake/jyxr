using Game.Core.Model;
using Game.Core.Persistence;

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
            .Where(timeKey => !timeKey.Triggered && IsAtOrAfter(clock, timeKey.DeadlineAt))
            .OrderBy(static timeKey => GetTotalTimeSlots(timeKey.DeadlineAt))
            .ThenBy(static timeKey => timeKey.Key, StringComparer.Ordinal)
            .ToArray();
        if (expired.Length == 0)
        {
            return [];
        }

        var triggeredAt = clock.ToRecord();
        var results = new List<ExpiredStoryTimeKey>(expired.Length);
        foreach (var timeKey in expired)
        {
            _session.State.Story.MarkTimeKeyTriggered(timeKey.Key, triggeredAt);
            results.Add(new ExpiredStoryTimeKey(timeKey.Key, timeKey.TargetStoryId));
        }

        _session.Events.Publish(new StoryStateChangedEvent());
        return results;
    }

    private static bool IsAtOrAfter(ClockState current, ClockRecord deadline) =>
        GetTotalTimeSlots(current) >= GetTotalTimeSlots(deadline);

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

public sealed record ExpiredStoryTimeKey(string Key, string TargetStoryId);
