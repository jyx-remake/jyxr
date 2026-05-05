using System.Reflection;

namespace Game.Core.Story;

public sealed class StoryCommandBinder
{
    private readonly object _target;
    private readonly Dictionary<string, MethodInfo> _commands = new(StringComparer.Ordinal);

    public StoryCommandBinder(object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;

        foreach (var method in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var attribute in method.GetCustomAttributes<StoryCommandAttribute>())
            {
                foreach (var name in attribute.Names)
                {
                    if (!_commands.TryAdd(name, method))
                    {
                        throw new InvalidOperationException($"Story command '{name}' is registered more than once.");
                    }
                }
            }
        }
    }

    public bool TryExecute(
        string name,
        IReadOnlyList<ExprValue> args,
        CancellationToken cancellationToken,
        out ValueTask result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(args);

        if (!_commands.TryGetValue(name, out var method))
        {
            result = default;
            return false;
        }

        var boundArgs = InvocationBinding.BindArguments(name, method, args, cancellationToken);
        object? rawResult;
        try
        {
            rawResult = method.Invoke(_target, boundArgs);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }

        result = rawResult switch
        {
            null => ValueTask.CompletedTask,
            ValueTask valueTask => valueTask,
            Task task => new ValueTask(task),
            _ => throw new InvalidOperationException(
                $"Story command '{name}' returned unsupported type '{rawResult.GetType().Name}'."),
        };
        return true;
    }
}
