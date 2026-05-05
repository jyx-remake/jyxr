using Game.Application;
using Game.Core.Model;
using Game.Core.Persistence;

namespace Game.Tests;

public sealed class ClockFormatterTests
{
    [Fact]
    public void ClockFormatter_FormatsChineseDateAndTimeSlot()
    {
        var clock = new ClockState();
        clock.AdvanceTimeSlots(10);
        clock.AdvanceDays(397);

        Assert.Equal("江湖二年二月九日", ClockFormatter.FormatDateCn(clock));
        Assert.Equal("寅时", ClockFormatter.FormatTimeSlotCn(clock.TimeSlot));
        Assert.Equal("江湖二年二月九日 寅时", ClockFormatter.FormatDateTimeCn(clock));
        Assert.Equal("江湖二年二月九日，寅时", ClockFormatter.FormatLogPrefixCn(clock));
    }

    [Fact]
    public void ClockFormatter_FormatsHudDateTime()
    {
        var clock = new ClockState();
        clock.AdvanceTimeSlots(2);

        Assert.Equal("江湖一年一月一日 午时", ClockFormatter.FormatDateTimeCn(clock));
    }

    [Fact]
    public void ClockFormatter_FormatsTensInDate()
    {
        var clock = ClockState.Restore(new ClockRecord(10, 12, 30, TimeSlot.Hai));

        Assert.Equal("江湖十年十二月三十日 亥时", ClockFormatter.FormatDateTimeCn(clock));
    }
}
