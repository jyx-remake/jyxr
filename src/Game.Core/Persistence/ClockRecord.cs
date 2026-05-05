using Game.Core.Model;

namespace Game.Core.Persistence;

public sealed record ClockRecord(
    int Year,
    int Month,
    int Day,
    TimeSlot TimeSlot);
