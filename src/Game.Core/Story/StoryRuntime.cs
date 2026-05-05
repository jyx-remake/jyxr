namespace Game.Core.Story;

public sealed class StoryRuntime
{
    public IAsyncEnumerable<StoryEvent> RunAsync(
        StoryScript script,
        IRuntimeHost host,
        string? startSegment = null,
        CancellationToken cancellationToken = default)
    {
        var session = new StoryRuntimeSession(script, host, startSegment, cancellationToken);
        return session.RunAsync();
    }
}
