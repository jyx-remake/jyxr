using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Story;

namespace Game.Application;

public sealed class StoryConditionEvaluator
{
    private readonly GameSession _session;
    private readonly IRuntimeHost _host;
    private readonly StoryVariableResolver _variableResolver;
    private readonly GamePredicateBinder _predicateBinder;

    public StoryConditionEvaluator(GameSession session, IRuntimeHost host)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(host);
        _session = session;
        _host = host;
        _variableResolver = new StoryVariableResolver(session);
        _predicateBinder = new GamePredicateBinder(new ApplicationPredicateLibrary(session));
    }

    private GameState State => _session.State;

    public ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_variableResolver.TryGetVariable(name, out var value))
        {
            return ValueTask.FromResult(value);
        }

        if (State.Story.TryGetVariable(name, out value))
        {
            return ValueTask.FromResult(value);
        }

        return _host.GetVariableAsync(name, cancellationToken);
    }

    public ValueTask<bool> EvaluatePredicateAsync(
        string name,
        IReadOnlyList<ExprValue> args,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(args);

        if (_predicateBinder.TryEvaluate(name, args, cancellationToken, out var result))
        {
            return result;
        }

        return _host.EvaluatePredicateAsync(name, args, cancellationToken);
    }
}
