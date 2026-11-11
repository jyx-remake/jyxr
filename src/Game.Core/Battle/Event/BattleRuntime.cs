using Game.Core.Story;
namespace Game.Core.Battle;

public sealed class BattleRuntime
{
    public IAsyncEnumerable<BattleEvent> RunAsync(
        BattleEventScript script,
        BattleMainSegment battleMainSegment,
        BattleState state,
        IStoryRuntimeContext host,
        string? startSegment = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        if (script.Version != BattleEventScript.CurrentVersion)
        {
            throw new StoryRuntimeException(
                $"Unsupported story script version '{script.Version}'. Expected version {BattleEventScript.CurrentVersion}.");
        }

        var session = new BattleEventRuntimeSession(script, battleMainSegment, state, host, startSegment, cancellationToken);
        return session.RunAsync();
    }
}