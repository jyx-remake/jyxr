using Game.Presentation.Collections;

namespace Game.Tests;

public sealed class VirtualGridLayoutTests
{
    [Fact]
    public void ExtentAndPositionsIncludeSpacingAndPartialLastRow()
    {
        var layout = new VirtualGridLayout(8, 3, 100, 100, 10, 10);

        Assert.Equal(3, layout.Rows);
        Assert.Equal(320, layout.Width);
        Assert.Equal(320, layout.Height);
        Assert.Equal((110f, 220f), layout.GetPosition(7));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(5_000)]
    [InlineData(500_000)]
    public void VisibleRangeIsBoundedByViewportAndOverscan(int count)
    {
        var layout = new VirtualGridLayout(count, 5, 100, 100, 10, 10);
        var (start, end) = layout.GetVisibleRange(layout.Height / 2, 220);

        Assert.InRange(start, 0, count - 1);
        Assert.InRange(end, start + 1, count);
        Assert.InRange(end - start, 15, 25);
        Assert.Equal(0, start % 5);
    }

    [Fact]
    public void VisibleRangeClampsScrollAfterItemsShrink()
    {
        var layout = new VirtualGridLayout(8, 3, 100, 100, 10, 10);

        Assert.Equal((0, 8), layout.GetVisibleRange(50_000, 200));
        Assert.Equal((0, 8), layout.GetVisibleRange(-100, 200));
        Assert.Equal((0, 0), layout.GetVisibleRange(0, 0));
        Assert.Equal((0, 0), new VirtualGridLayout(0, 3, 100, 100, 10, 10).GetVisibleRange(0, 200));
    }

    [Theory]
    [InlineData(-100, -100, 0)]
    [InlineData(49, 50, 0)]
    [InlineData(50, 50, 1)]
    [InlineData(500, 50, 3)]
    [InlineData(0, 105, 0)]
    [InlineData(0, 106, 3)]
    [InlineData(0, 160, 3)]
    [InlineData(49, 270, 6)]
    [InlineData(500, 50_000, 8)]
    public void InsertionSlotUsesFullLayoutWithoutRealizedCards(float x, float y, int expected)
    {
        var layout = new VirtualGridLayout(8, 3, 100, 100, 10, 10);

        Assert.Equal(expected, layout.GetInsertionSlot(x, y));
    }

    [Fact]
    public void EmptyGridHasNoRowsAndAnInitialInsertionSlot()
    {
        var layout = new VirtualGridLayout(0, 3, 100, 100, 10, 10);

        Assert.Equal(0, layout.Rows);
        Assert.Equal(0, layout.Width);
        Assert.Equal(0, layout.Height);
        Assert.Equal(0, layout.GetInsertionSlot(500, 500));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetPosition(0));
    }
}
