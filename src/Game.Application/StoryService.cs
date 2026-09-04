using Game.Core.Story;

namespace Game.Application;

public sealed class StoryService
{
    private readonly GameSession _session;
    private readonly StoryRuntime _runtime = new();
    private readonly GameExpressionEnvironment _expressions;

    public StoryService(GameSession session, IRuntimeHost host)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        Host = host ?? throw new ArgumentNullException(nameof(host));
        CommandDispatcher = new StoryCommandDispatcher(session, host);
        CommandLine = new StoryCommandLineService(CommandDispatcher);
        _expressions = new GameExpressionEnvironment(session);
    }

    public IRuntimeHost Host { get; }
    public StoryCommandDispatcher CommandDispatcher { get; }
    public StoryCommandLineService CommandLine { get; }

    public async Task ExecuteAsync(
        string storyId,
        StoryExecutionContext? context = null,
        CancellationToken cancellationToken = default)
    {
        await foreach (var _ in RunAsync(storyId, context, cancellationToken)) { }
    }

    public async IAsyncEnumerable<StoryEvent> RunAsync(
        string storyId,
        StoryExecutionContext? context = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storyId);
        storyId = ResolveStoryReference(storyId);
        var executionState = _session.State;
        var entry = _session.ContentRepository.GetStorySegment(storyId);
        var executionContext = context ?? StoryExecutionContext.Empty;
        var runtimeDispatcher = new StoryCommandDispatcher(_session, Host, executionContext, includeDebugCommands: false);
        var runtimeHost = new ApplicationStoryRuntimeHost(
            _session,
            executionState,
            Host,
            runtimeDispatcher,
            new StoryTextInterpolator(_session),
            _expressions.Create(executionContext));

        await foreach (var storyEvent in _runtime.RunAsync(entry.Script, runtimeHost, entry.Segment.Name, cancellationToken))
        {
            if (storyEvent is SegmentCompletedEvent completed)
            {
                executionState.Story.MarkCompleted(completed.SegmentId, executionState.Clock);
                executionState.Story.SetLastStory(completed.SegmentId);
            }
            yield return storyEvent;
        }
    }

    /// <summary>
    /// Legacy story references occasionally contain a pipe-separated list
    /// meaning "run one of these at random" (legacy MapUI.LoadStory). An
    /// exact segment id always wins; otherwise one trimmed candidate is
    /// picked through the session random service.
    /// </summary>
    private string ResolveStoryReference(string storyId)
    {
        if (!storyId.Contains('|') || _session.ContentRepository.TryGetStorySegment(storyId, out _))
        {
            return storyId;
        }

        var candidates = storyId
            .Split('|')
            .Select(candidate => candidate.Trim())
            .Where(candidate => candidate.Length > 0)
            .ToList();
        if (candidates.Count == 0)
        {
            return storyId;
        }

        return candidates[_session.RandomService.Next(0, candidates.Count)];
    }
}
