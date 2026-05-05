using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class ClockState
{
    public const int TimeSlotsPerDay = 12;
    public const int DaysPerMonth = 30;
    public const int MonthsPerYear = 12;
    public const int DaysPerYear = DaysPerMonth * MonthsPerYear;

    public int Year { get; private set; } = 1;

    public int Month { get; private set; } = 1;

    public int Day { get; private set; } = 1;

    public int TotalDays => checked((Year - 1) * DaysPerYear + (Month - 1) * DaysPerMonth + Day - 1);

    public TimeSlot TimeSlot { get; private set; } = TimeSlot.Chen;

    public static ClockState Restore(ClockRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentOutOfRangeException.ThrowIfLessThan(record.Year, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(record.Month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(record.Month, MonthsPerYear);
        ArgumentOutOfRangeException.ThrowIfLessThan(record.Day, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(record.Day, DaysPerMonth);
        if (!Enum.IsDefined(record.TimeSlot))
        {
            throw new ArgumentOutOfRangeException(nameof(record));
        }

        return new ClockState
        {
            Year = record.Year,
            Month = record.Month,
            Day = record.Day,
            TimeSlot = record.TimeSlot,
        };
    }

    public void AdvanceTimeSlots(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var totalSlots = (int)TimeSlot + amount;
        var elapsedDays = totalSlots / TimeSlotsPerDay;
        TimeSlot = (TimeSlot)(totalSlots % TimeSlotsPerDay);
        AdvanceDays(elapsedDays);
    }

    public void AdvanceDays(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var totalDays = checked(TotalDays + amount);
        Year = totalDays / DaysPerYear + 1;
        var dayOfYear = totalDays % DaysPerYear;
        Month = dayOfYear / DaysPerMonth + 1;
        Day = dayOfYear % DaysPerMonth + 1;
    }

    public void AdvanceMonths(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        AdvanceDays(checked(amount * DaysPerMonth));
    }

    public bool InTimeSlot(params TimeSlot[] timeSlots)
    {
        ArgumentNullException.ThrowIfNull(timeSlots);
        return timeSlots.Contains(TimeSlot);
    }

    public ClockRecord ToRecord() => new(Year, Month, Day, TimeSlot);
}

public enum TimeSlot
{
    Zi = 0,
    Chou = 1,
    Yin = 2,
    Mao = 3,
    Chen = 4,
    Si = 5,
    Wu = 6,
    Wei = 7,
    Shen = 8,
    You = 9,
    Xu = 10,
    Hai = 11,
}
