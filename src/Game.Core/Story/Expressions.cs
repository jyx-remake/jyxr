using System.Globalization;

namespace Game.Core.Story;

public abstract record ExprNode;

public sealed record LiteralExprNode(ExprValue Value) : ExprNode;

public sealed record VariableExprNode(string Name) : ExprNode;

public sealed record PredicateExprNode(
    string Name,
    IReadOnlyList<ExprNode> Arguments) : ExprNode;

public sealed record NotExprNode(ExprNode Operand) : ExprNode;

public sealed record BinaryExprNode(
    ExprBinaryOperator Operator,
    ExprNode Left,
    ExprNode Right) : ExprNode;

public enum ExprBinaryOperator
{
    And,
    Or,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
}

public enum ExprValueKind
{
    Boolean,
    Number,
    String,
}

public readonly record struct ExprValue
{
    private ExprValue(ExprValueKind kind, bool boolean, double number, string? text)
    {
        Kind = kind;
        Boolean = boolean;
        Number = number;
        Text = text;
    }

    public ExprValueKind Kind { get; }

    public bool Boolean { get; }

    public double Number { get; }

    public string? Text { get; }

    public static ExprValue FromBoolean(bool value) => new(ExprValueKind.Boolean, value, default, null);

    public static ExprValue FromNumber(double value) => new(ExprValueKind.Number, default, value, null);

    public static ExprValue FromString(string value) => new(ExprValueKind.String, default, default, value);

    public bool AsBoolean(string context)
    {
        if (Kind != ExprValueKind.Boolean)
        {
            throw new StoryRuntimeException($"{context} requires a boolean value, got {DescribeKind()}.");
        }

        return Boolean;
    }

    public double AsNumber(string context)
    {
        if (Kind != ExprValueKind.Number)
        {
            throw new StoryRuntimeException($"{context} requires a numeric value, got {DescribeKind()}.");
        }

        return Number;
    }

    public string AsString(string context)
    {
        if (Kind != ExprValueKind.String || Text is null)
        {
            throw new StoryRuntimeException($"{context} requires a string value, got {DescribeKind()}.");
        }

        return Text;
    }

    public string DescribeKind() => Kind switch
    {
        ExprValueKind.Boolean => "boolean",
        ExprValueKind.Number => "number",
        ExprValueKind.String => "string",
        _ => "unknown",
    };

    public override string ToString() => Kind switch
    {
        ExprValueKind.Boolean => Boolean ? "true" : "false",
        ExprValueKind.Number => Number.ToString(CultureInfo.InvariantCulture),
        ExprValueKind.String => Text ?? string.Empty,
        _ => string.Empty,
    };
}
