using Game.Core.Definitions;
using Game.Core.Story;

namespace Game.Application;

public sealed class MapConditionEvaluator
{
    private readonly GamePredicateBinder _predicateBinder;
    private readonly MapConditionInvocationParser _parser = new();

    public MapConditionEvaluator(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _predicateBinder = new GamePredicateBinder(new ApplicationPredicateLibrary(session));
    }

    public bool AreSatisfied(IReadOnlyList<MapEventConditionDefinition> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        foreach (var condition in conditions)
        {
            if (!IsSatisfied(condition))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsSatisfied(MapEventConditionDefinition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentException.ThrowIfNullOrWhiteSpace(condition.Type);

        var invocation = _parser.Parse(condition);
        if (!_predicateBinder.TryEvaluate(invocation.Name, invocation.Arguments, CancellationToken.None, out var result))
        {
            throw new InvalidOperationException($"Unsupported map condition type: {condition.Type}");
        }

        return result.GetAwaiter().GetResult();
    }
}
