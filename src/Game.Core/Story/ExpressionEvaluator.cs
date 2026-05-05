namespace Game.Core.Story;

internal static class ExpressionEvaluator
{
    public static async ValueTask<ExprValue> EvaluateAsync(
        ExprNode expr,
        IRuntimeHost host,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (TryEvaluateLiteralOrVariable(expr, host, cancellationToken, out var valueTask))
        {
            return await valueTask;
        }

        return expr switch
        {
            PredicateExprNode predicate => ExprValue.FromBoolean(await EvaluatePredicateAsync(predicate, host, cancellationToken)),
            NotExprNode notExpr => ExprValue.FromBoolean(!((await EvaluateAsync(notExpr.Operand, host, cancellationToken)).AsBoolean("not"))),
            BinaryExprNode binary => await EvaluateBinaryAsync(binary, host, cancellationToken),
            _ => throw new StoryRuntimeException($"Unsupported expression node '{expr.GetType().Name}'."),
        };
    }

    private static async ValueTask<bool> EvaluatePredicateAsync(
        PredicateExprNode predicate,
        IRuntimeHost host,
        CancellationToken cancellationToken)
    {
        var args = new List<ExprValue>(predicate.Arguments.Count);
        foreach (var argument in predicate.Arguments)
        {
            args.Add(await EvaluateValueArgAsync(argument, host, cancellationToken));
        }

        return await host.EvaluatePredicateAsync(predicate.Name, args, cancellationToken);
    }

    public static async ValueTask<ExprValue> EvaluateValueArgAsync(
        ExprNode arg,
        IRuntimeHost host,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (TryEvaluateLiteralOrVariable(arg, host, cancellationToken, out var valueTask))
        {
            return await valueTask;
        }

        throw new StoryRuntimeException("Value arguments must be literals or variables.");
    }

    private static bool TryEvaluateLiteralOrVariable(
        ExprNode expr,
        IRuntimeHost host,
        CancellationToken cancellationToken,
        out ValueTask<ExprValue> valueTask)
    {
        switch (expr)
        {
            case LiteralExprNode literal:
                valueTask = ValueTask.FromResult(literal.Value);
                return true;
            case VariableExprNode variable:
                valueTask = host.GetVariableAsync(variable.Name, cancellationToken);
                return true;
            default:
                valueTask = default;
                return false;
        }
    }

    private static async ValueTask<ExprValue> EvaluateBinaryAsync(
        BinaryExprNode binary,
        IRuntimeHost host,
        CancellationToken cancellationToken)
    {
        switch (binary.Operator)
        {
            case ExprBinaryOperator.And:
            {
                var left = (await EvaluateAsync(binary.Left, host, cancellationToken)).AsBoolean("and");
                if (!left)
                {
                    return ExprValue.FromBoolean(false);
                }

                var right = (await EvaluateAsync(binary.Right, host, cancellationToken)).AsBoolean("and");
                return ExprValue.FromBoolean(right);
            }
            case ExprBinaryOperator.Or:
            {
                var left = (await EvaluateAsync(binary.Left, host, cancellationToken)).AsBoolean("or");
                if (left)
                {
                    return ExprValue.FromBoolean(true);
                }

                var right = (await EvaluateAsync(binary.Right, host, cancellationToken)).AsBoolean("or");
                return ExprValue.FromBoolean(right);
            }
            case ExprBinaryOperator.Equal:
            {
                var (left, right) = await EvaluateBothAsync(binary, host, cancellationToken);
                return ExprValue.FromBoolean(CompareEquality(left, right));
            }
            case ExprBinaryOperator.NotEqual:
            {
                var (left, right) = await EvaluateBothAsync(binary, host, cancellationToken);
                return ExprValue.FromBoolean(!CompareEquality(left, right));
            }
            case ExprBinaryOperator.GreaterThan:
            {
                var (left, right) = await EvaluateNumbersAsync(binary, host, cancellationToken);
                return ExprValue.FromBoolean(left > right);
            }
            case ExprBinaryOperator.GreaterThanOrEqual:
            {
                var (left, right) = await EvaluateNumbersAsync(binary, host, cancellationToken);
                return ExprValue.FromBoolean(left >= right);
            }
            case ExprBinaryOperator.LessThan:
            {
                var (left, right) = await EvaluateNumbersAsync(binary, host, cancellationToken);
                return ExprValue.FromBoolean(left < right);
            }
            case ExprBinaryOperator.LessThanOrEqual:
            {
                var (left, right) = await EvaluateNumbersAsync(binary, host, cancellationToken);
                return ExprValue.FromBoolean(left <= right);
            }
            default:
                throw new StoryRuntimeException($"Unsupported binary operator '{binary.Operator}'.");
        }
    }

    private static async ValueTask<(ExprValue Left, ExprValue Right)> EvaluateBothAsync(
        BinaryExprNode binary,
        IRuntimeHost host,
        CancellationToken cancellationToken)
    {
        var left = await EvaluateAsync(binary.Left, host, cancellationToken);
        var right = await EvaluateAsync(binary.Right, host, cancellationToken);
        return (left, right);
    }

    private static async ValueTask<(double Left, double Right)> EvaluateNumbersAsync(
        BinaryExprNode binary,
        IRuntimeHost host,
        CancellationToken cancellationToken)
    {
        var (left, right) = await EvaluateBothAsync(binary, host, cancellationToken);
        return (left.AsNumber(binary.Operator.ToString()), right.AsNumber(binary.Operator.ToString()));
    }

    private static bool CompareEquality(ExprValue left, ExprValue right)
    {
        if (left.Kind != right.Kind)
        {
            throw new StoryRuntimeException(
                $"Equality comparison requires values of the same type, got {left.DescribeKind()} and {right.DescribeKind()}.");
        }

        return left.Kind switch
        {
            ExprValueKind.Boolean => left.Boolean == right.Boolean,
            ExprValueKind.Number => left.Number.Equals(right.Number),
            ExprValueKind.String => string.Equals(left.Text, right.Text, StringComparison.Ordinal),
            _ => throw new StoryRuntimeException($"Unsupported equality comparison type '{left.Kind}'."),
        };
    }
}
