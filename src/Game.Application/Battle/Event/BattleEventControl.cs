using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Story;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using static System.Collections.Specialized.BitVector32;

namespace Game.Application;

public class BattleEventControl
{

    private readonly GameSession _session;
    private readonly BattleRuntime _runtime;
    private  GameExpressionEnvironment? _expressions;
    public IRuntimeHost Host { get; }
    public BattleEventControl(GameSession session, BattleRuntime runtime, IRuntimeHost host)
    {
        _session = session;
        _runtime = runtime;
        Host = host;
    }
    public async IAsyncEnumerable<BattleEvent> RunAsync(
    BattleEventSegmentEntry entry,
    BattleMainSegment battleEventSegment,
    BattleState state,
    StoryExecutionContext? context = null,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var executionState = _session.State;
        var executionContext = context ?? StoryExecutionContext.Empty;
        _expressions = new GameExpressionEnvironment(_session, state);
        var runtimeDispatcher = new StoryCommandDispatcher(_session, Host, executionContext, includeDebugCommands: false, battleState: state);
        var runtimeHost = new ApplicationStoryRuntimeHost(
            _session,
            executionState,
            Host,
            runtimeDispatcher,
            new StoryTextInterpolator(_session),
            _expressions.Create(executionContext)
            );

        await foreach (var battleEvent in _runtime.RunAsync(entry.Script, battleEventSegment, state, runtimeHost, entry.Segment.Name, cancellationToken))
        {
            if (battleEvent is Core.Battle.SegmentCompletedEvent completed)
            {
                executionState.Story.MarkCompleted(completed.SegmentId);
                executionState.Story.SetLastStory(completed.SegmentId);
            }
            yield return battleEvent;
        }
    }
}
