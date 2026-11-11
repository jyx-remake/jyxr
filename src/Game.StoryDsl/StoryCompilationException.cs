namespace Game.StoryDsl;

public sealed class StoryCompilationException : Exception
{
    internal StoryCompilationException(
        string sourceName,
        string? segmentName,
        int line,
        int column,
        string message,
        Exception? innerException = null)
        : base($"{sourceName}({line},{column})" +
               (segmentName is null ? "" : $" [剧情段 '{segmentName}']") +
               $": {message}", innerException)
    {
        SourceName = sourceName;
        SegmentName = segmentName;
        Line = line;
        Column = column;
    }

    public string SourceName { get; }
    public string? SegmentName { get; }
    public int Line { get; }
    public int Column { get; }
}
