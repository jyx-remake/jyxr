using Game.Core.Story;

namespace Game.Application;

public sealed class StoryService
{
    private readonly GameSession _session;
    private readonly StoryRuntime _runtime = new();
    private readonly ApplicationStoryRuntimeHost _runtimeHost;

    public StoryService(GameSession session, IRuntimeHost host)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(host);

        _session = session;
        Host = host;
        ConditionEvaluator = new StoryConditionEvaluator(session, host);
        CommandDispatcher = new StoryCommandDispatcher(session, host);
        CommandLine = new StoryCommandLineService(CommandDispatcher);
        _runtimeHost = new ApplicationStoryRuntimeHost(
            host,
            ConditionEvaluator,
            CommandDispatcher,
            new StoryTextInterpolator(session));
    }

    private Game.Core.Model.GameState State => _session.State;

    public IRuntimeHost Host { get; }

    public StoryConditionEvaluator ConditionEvaluator { get; }

    public StoryCommandDispatcher CommandDispatcher { get; }

    public StoryCommandLineService CommandLine { get; }

    public async Task ExecuteAsync(
        string storyId,
        CancellationToken cancellationToken = default)
    {
        await foreach (var _ in RunAsync(storyId, cancellationToken))
        {
        }
    }

    public async IAsyncEnumerable<StoryEvent> RunAsync(
        string storyId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storyId);

        var entry = _session.ContentRepository.GetStorySegment(storyId);

        await foreach (var storyEvent in _runtime.RunAsync(entry.Script, _runtimeHost, entry.Segment.Name, cancellationToken))
        {
            switch (storyEvent)
            {
                case SegmentCompletedEvent completed:
                    State.Story.MarkCompleted(completed.SegmentId);
                    State.Story.SetLastStory(completed.SegmentId);
                    break;
            }

            yield return storyEvent;
        }
    }
}
