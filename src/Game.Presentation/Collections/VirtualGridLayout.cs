namespace Game.Presentation.Collections;

public readonly struct VirtualGridLayout
{
    public VirtualGridLayout(
        int count,
        int columns,
        float cellWidth,
        float cellHeight,
        float horizontalSpacing,
        float verticalSpacing)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(horizontalSpacing);
        ArgumentOutOfRangeException.ThrowIfNegative(verticalSpacing);
        Count = count;
        Columns = columns;
        CellWidth = cellWidth;
        CellHeight = cellHeight;
        HorizontalSpacing = horizontalSpacing;
        VerticalSpacing = verticalSpacing;
    }

    public int Count { get; }
    public int Columns { get; }
    public float CellWidth { get; }
    public float CellHeight { get; }
    public float HorizontalSpacing { get; }
    public float VerticalSpacing { get; }
    public int Rows => Count == 0 ? 0 : (Count - 1) / Columns + 1;
    public float Width => Count == 0 ? 0 : Math.Min(Columns, Count) * (CellWidth + HorizontalSpacing) - HorizontalSpacing;
    public float Height => Rows == 0 ? 0 : Rows * (CellHeight + VerticalSpacing) - VerticalSpacing;

    public (float X, float Y) GetPosition(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
        return (index % Columns * (CellWidth + HorizontalSpacing),
            index / Columns * (CellHeight + VerticalSpacing));
    }

    public (int Start, int End) GetVisibleRange(float scrollOffset, float viewportHeight)
    {
        if (Count == 0 || viewportHeight <= 0)
        {
            return (0, 0);
        }

        var offset = Math.Clamp(scrollOffset, 0, Math.Max(0, Height - viewportHeight));
        var rowStride = CellHeight + VerticalSpacing;
        var firstRow = Math.Max(0, (int)Math.Floor(offset / rowStride) - 1);
        var endRow = Math.Min(Rows, (int)Math.Ceiling((offset + viewportHeight) / rowStride) + 1);
        return (firstRow * Columns, (int)Math.Min(Count, (long)endRow * Columns));
    }

    public int GetInsertionSlot(float x, float y)
    {
        if (Count == 0)
        {
            return 0;
        }

        var row = Math.Clamp(
            (int)Math.Ceiling((y - CellHeight / 2) / (CellHeight + VerticalSpacing) - 0.5f),
            0,
            Rows - 1);
        var rowStart = row * Columns;
        var rowCount = Math.Min(Columns, Count - rowStart);
        var column = Math.Clamp(
            (int)Math.Ceiling((x - CellWidth / 2) / (CellWidth + HorizontalSpacing) - 0.5f),
            0,
            rowCount - 1);
        var centerX = column * (CellWidth + HorizontalSpacing) + CellWidth / 2;
        return rowStart + column + (x < centerX ? 0 : 1);
    }
}
