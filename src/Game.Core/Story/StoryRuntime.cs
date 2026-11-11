namespace Game.Core.Story;

public sealed class StoryRuntime
{
    public IAsyncEnumerable<StoryEvent> RunAsync(
        StoryScript script,
        IStoryRuntimeContext host,
        string? startSegment = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        var session = new StoryRuntimeSession(script, host, startSegment, cancellationToken);
        return session.RunAsync();
    }
}
