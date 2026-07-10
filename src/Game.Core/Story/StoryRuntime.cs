namespace Game.Core.Story;

public sealed class StoryRuntime
{
    public IAsyncEnumerable<StoryEvent> RunAsync(
        StoryScript script,
        IRuntimeHost host,
        string? startSegment = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        if (script.Version != StoryScript.CurrentVersion)
        {
            throw new StoryRuntimeException(
                $"Unsupported story script version '{script.Version}'. Expected version {StoryScript.CurrentVersion}.");
        }

        var session = new StoryRuntimeSession(script, host, startSegment, cancellationToken);
        return session.RunAsync();
    }
}
