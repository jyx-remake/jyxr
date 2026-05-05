using System.Reflection;

namespace Game.Core.Story;

public sealed class GamePredicateBinder
{
    private readonly object _target;
    private readonly Dictionary<string, MethodInfo> _predicates = new(StringComparer.Ordinal);

    public GamePredicateBinder(object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;

        foreach (var method in target.GetType().GetMethods(
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var attribute in method.GetCustomAttributes<GamePredicateAttribute>())
            {
                foreach (var name in attribute.Names)
                {
                    if (!_predicates.TryAdd(name, method))
                    {
                        throw new InvalidOperationException($"Game predicate '{name}' is registered more than once.");
                    }
                }
            }
        }
    }

    public bool TryEvaluate(
        string name,
        IReadOnlyList<ExprValue> args,
        CancellationToken cancellationToken,
        out ValueTask<bool> result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(args);

        if (!_predicates.TryGetValue(name, out var method))
        {
            result = default;
            return false;
        }

        var boundArgs = InvocationBinding.BindArguments(name, method, args, cancellationToken);
        object? rawResult;
        try
        {
            rawResult = method.Invoke(method.IsStatic ? null : _target, boundArgs);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }

        result = rawResult switch
        {
            bool boolean => ValueTask.FromResult(boolean),
            ValueTask<bool> valueTask => valueTask,
            Task<bool> task => new ValueTask<bool>(task),
            _ => throw new InvalidOperationException(
                $"Game predicate '{name}' returned unsupported type '{rawResult?.GetType().Name ?? "null"}'."),
        };
        return true;
    }
}
