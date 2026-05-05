using System.Reflection;

namespace Game.Core.Story;

internal static class InvocationBinding
{
    public static object?[] BindArguments(
        string invocationName,
        MethodInfo method,
        IReadOnlyList<ExprValue> args,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invocationName);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(args);

        var parameters = method.GetParameters();
        var values = new object?[parameters.Length];
        var argumentIndex = 0;

        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex += 1)
        {
            var parameter = parameters[parameterIndex];
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                values[parameterIndex] = cancellationToken;
                continue;
            }

            if (parameter.GetCustomAttribute<ParamArrayAttribute>() is not null)
            {
                values[parameterIndex] = BindParamArray(invocationName, parameter, args, ref argumentIndex);
                continue;
            }

            if (argumentIndex >= args.Count)
            {
                if (parameter.HasDefaultValue)
                {
                    values[parameterIndex] = parameter.DefaultValue;
                    continue;
                }

                throw new InvalidOperationException(
                    $"Invocation '{invocationName}' is missing argument '{parameter.Name ?? parameterIndex.ToString()}'.");
            }

            values[parameterIndex] = ConvertArgument(
                invocationName,
                parameter.ParameterType,
                parameter.Name ?? parameterIndex.ToString(),
                args[argumentIndex],
                argumentIndex);
            argumentIndex += 1;
        }

        if (argumentIndex < args.Count)
        {
            throw new InvalidOperationException($"Invocation '{invocationName}' received too many arguments.");
        }

        return values;
    }

    private static Array BindParamArray(
        string invocationName,
        ParameterInfo parameter,
        IReadOnlyList<ExprValue> args,
        ref int argumentIndex)
    {
        var elementType = parameter.ParameterType.GetElementType()
            ?? throw new InvalidOperationException(
                $"Invocation '{invocationName}' params parameter '{parameter.Name}' must be an array.");
        var remainingCount = args.Count - argumentIndex;
        var array = Array.CreateInstance(elementType, remainingCount);
        for (var index = 0; index < remainingCount; index += 1)
        {
            array.SetValue(
                ConvertArgument(
                    invocationName,
                    elementType,
                    parameter.Name ?? index.ToString(),
                    args[argumentIndex + index],
                    argumentIndex + index),
                index);
        }

        argumentIndex += remainingCount;
        return array;
    }

    private static object ConvertArgument(
        string invocationName,
        Type targetType,
        string parameterName,
        ExprValue value,
        int index)
    {
        var context = $"Invocation '{invocationName}' argument '{parameterName}'";
        if (targetType == typeof(string))
        {
            return value.AsString(context);
        }

        if (targetType == typeof(int))
        {
            var number = value.AsNumber(context);
            if (number % 1d != 0d)
            {
                throw new InvalidOperationException(
                    $"Invocation '{invocationName}' requires an integer at index {index}.");
            }

            return checked((int)number);
        }

        if (targetType == typeof(double))
        {
            return value.AsNumber(context);
        }

        if (targetType == typeof(bool))
        {
            return value.AsBoolean(context);
        }

        if (targetType == typeof(ExprValue))
        {
            return value;
        }

        throw new InvalidOperationException(
            $"Invocation '{invocationName}' parameter '{parameterName}' has unsupported type '{targetType.Name}'.");
    }
}
