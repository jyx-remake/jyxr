using Game.Core.Model;

namespace Game.Application;

public static class ClockFormatter
{
    private static readonly string[] ChineseDigits =
    [
        "零",
        "一",
        "二",
        "三",
        "四",
        "五",
        "六",
        "七",
        "八",
        "九",
    ];

    public static string FormatDateCn(ClockState clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return $"江湖{FormatPositiveIntegerCn(clock.Year)}年{FormatPositiveIntegerCn(clock.Month)}月{FormatPositiveIntegerCn(clock.Day)}日";
    }

    public static string FormatDateTimeCn(ClockState clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return $"{FormatDateCn(clock)} {FormatTimeSlotCn(clock.TimeSlot)}";
    }

    public static string FormatLogPrefixCn(ClockState clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return $"{FormatDateCn(clock)}，{FormatTimeSlotCn(clock.TimeSlot)}";
    }

    public static string FormatTimeSlotCn(TimeSlot timeSlot) =>
        timeSlot switch
        {
            TimeSlot.Zi => "子时",
            TimeSlot.Chou => "丑时",
            TimeSlot.Yin => "寅时",
            TimeSlot.Mao => "卯时",
            TimeSlot.Chen => "辰时",
            TimeSlot.Si => "巳时",
            TimeSlot.Wu => "午时",
            TimeSlot.Wei => "未时",
            TimeSlot.Shen => "申时",
            TimeSlot.You => "酉时",
            TimeSlot.Xu => "戌时",
            TimeSlot.Hai => "亥时",
            _ => throw new ArgumentOutOfRangeException(nameof(timeSlot), timeSlot, null),
        };

    private static string FormatPositiveIntegerCn(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        var groupUnits = new[] { string.Empty, "万", "亿" };
        var groups = new List<int>();
        var remaining = value;
        while (remaining > 0)
        {
            groups.Add(remaining % 10_000);
            remaining /= 10_000;
        }

        var result = string.Empty;
        var pendingZero = false;
        for (var i = groups.Count - 1; i >= 0; i--)
        {
            var group = groups[i];
            if (group == 0)
            {
                pendingZero = result.Length > 0;
                continue;
            }

            var isLeadingGroup = result.Length == 0;
            if (!isLeadingGroup && (pendingZero || group < 1_000))
            {
                result += ChineseDigits[0];
            }

            result += FormatIntegerGroupCn(group, isLeadingGroup);
            result += groupUnits[i];
            pendingZero = false;
        }

        return result;
    }

    private static string FormatIntegerGroupCn(int value, bool omitLeadingOneForTen)
    {
        var unitValues = new[] { 1_000, 100, 10, 1 };
        var unitTexts = new[] { "千", "百", "十", string.Empty };
        var result = string.Empty;
        var pendingZero = false;

        for (var i = 0; i < unitValues.Length; i++)
        {
            var unitValue = unitValues[i];
            var digit = value / unitValue % 10;
            if (digit == 0)
            {
                pendingZero = result.Length > 0;
                continue;
            }

            if (pendingZero)
            {
                result += ChineseDigits[0];
                pendingZero = false;
            }

            if (digit != 1 || unitValue != 10 || result.Length > 0 || !omitLeadingOneForTen)
            {
                result += ChineseDigits[digit];
            }

            result += unitTexts[i];
        }

        return result;
    }
}
