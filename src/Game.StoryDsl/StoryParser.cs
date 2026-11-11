using Game.Core.Story;
using Game.Expressions;

namespace Game.StoryDsl;

internal sealed partial class StoryParser
{
    private static readonly HashSet<string> ReservedCommands = new(StringComparer.Ordinal)
    {
        "if", "elif", "else", "battle", "jump", "call", "return", "del", "win", "lose", "timeout",
    };

    private readonly ExpressionParser _expressions = new();
    private readonly List<StoryLine> _lines;
    private readonly string _sourceName;
    private int _index;
    private string? _segmentName;

    public StoryParser(string source, string sourceName)
    {
        _sourceName = sourceName;
        _lines = ReadLines(source);
    }

    public StoryScript Parse()
    {
        var segments = new List<Segment>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        while (Peek() is { } line)
        {
            if (!line.IsHeader)
            {
                throw Error(line, "剧情段必须以顶格 '# 段名' 开始。");
            }

            _segmentName = line.Text[1..].Trim();
            if (_segmentName.Length == 0)
            {
                throw Error(line, "剧情段名不能为空。");
            }
            if (!names.Add(_segmentName))
            {
                throw Error(line, $"重复的剧情段名 '{_segmentName}'。");
            }

            _index++;
            segments.Add(new Segment(_segmentName, ParseSteps(0)));
        }

        return new StoryScript(segments.ToArray());
    }

    private IReadOnlyList<Step> ParseSteps(int indent)
    {
        var steps = new List<Step>();
        var terminated = false;
        while (Peek() is { } line && !line.IsHeader && line.Indent >= indent)
        {
            if (line.Indent != indent)
            {
                throw Error(line, "出现了意外的缩进层级，请使用 2 个空格缩进一级。");
            }
            if (terminated)
            {
                throw Error(line, "jump/return 之后的同级语句不可达。");
            }

            var step = ParseStep(line);
            steps.Add(step);
            terminated = step is JumpStep or ReturnStep;
        }

        return steps.ToArray();
    }

    private Step ParseStep(StoryLine line)
    {
        if (line.IsKeyword("if"))
        {
            return ParseBranch(line.Indent);
        }
        if (line.IsKeyword("elif") || line.IsKeyword("else"))
        {
            throw Error(line, "elif/else 必须紧跟在同级 if 之后。");
        }
        if (line.IsKeyword("battle"))
        {
            return ParseBattle(line);
        }
        if (line.IsOption)
        {
            throw Error(line, "'- xxx' 只能作为 choice 或 battle 的子结构出现。");
        }

        _index++;
        if (line.IsKeyword("jump") || line.IsKeyword("call"))
        {
            var keyword = line.IsKeyword("jump") ? "jump" : "call";
            var target = ReadArgument(line, keyword);
            return keyword == "jump" ? new JumpStep(target) : new CallStep(target);
        }
        if (line.IsKeyword("return"))
        {
            EnsureNoArgument(line, "return");
            return new ReturnStep();
        }
        if (line.IsKeyword("del"))
        {
            var target = ReadArgument(line, "del");
            ValidateVariable(line, target);
            return new DeleteVariableStep(target);
        }
        if (TryParseAssignment(line) is { } assignment)
        {
            return assignment;
        }

        var separator = FindDialogueSeparator(line.Text);
        if (separator >= 0)
        {
            return ParseDialogue(line, separator);
        }

        var call = ParseCall(line);
        var syntax = call.Root;
        if (ReservedCommands.Contains(syntax.Name))
        {
            throw Error(line, $"'{syntax.Name}' 是 DSL 保留字，不能作为命令名。");
        }

        if (syntax.Name == "maxlevel" && syntax.Arguments.Count == 2)
        {
            if (syntax.Arguments[0] is not LiteralExpressionSyntax { Value.Kind: ExpressionValueKind.String } skill)
            {
                throw Error(line, "maxlevel 自动补 key 时，技能名必须是字符串字面量。");
            }

            var onceKey = new LiteralExpressionSyntax(
                ExpressionValue.FromString($"{_segmentName}_{skill.Value.Text}"), skill.Span);
            call = call with { Root = syntax with { Arguments = [.. syntax.Arguments, onceKey] } };
        }

        return new CommandStep(call);
    }

    private SetVariableStep? TryParseAssignment(StoryLine line)
    {
        var end = 0;
        while (end < line.Text.Length && ExpressionSymbol.IsIdentifierPart(line.Text[end]))
        {
            end++;
        }
        if (end == 0)
        {
            return null;
        }

        var rest = line.Text[end..].TrimStart();
        var operation = rest.StartsWith("+=", StringComparison.Ordinal) ? "+="
            : rest.StartsWith("-=", StringComparison.Ordinal) ? "-="
            : rest.StartsWith('=') && !rest.StartsWith("==", StringComparison.Ordinal) ? "=" : null;
        if (operation is null)
        {
            return null;
        }

        var target = line.Text[..end];
        ValidateVariable(line, target);
        var value = rest[operation.Length..].TrimStart();
        var parsedValue = ParseExpression(line, value, line.Text.Length - value.Length);
        if (operation != "=")
        {
            value = $"{target} {operation[0]} ({value})";
            parsedValue = _expressions.ParseExpression(value, ExpressionSourceName(line, end));
        }

        return new SetVariableStep(target, parsedValue);
    }

    private BattleStep ParseBattle(StoryLine header)
    {
        var battleId = ReadArgument(header, "battle");
        _index++;
        var outcomes = new Dictionary<BattleOutcome, IReadOnlyList<Step>>();
        while (Peek() is { } line && line.Indent == header.Indent && line.IsOption)
        {
            var outcomeName = line.Text[1..].Trim();
            var outcome = outcomeName switch
            {
                "win" => BattleOutcome.Win,
                "lose" => BattleOutcome.Lose,
                "timeout" => BattleOutcome.Timeout,
                _ => throw Error(line, "battle 分支只允许 win / lose / timeout。"),
            };
            if (outcomes.ContainsKey(outcome))
            {
                throw Error(line, $"battle 结果分支 '{outcomeName}' 重复。");
            }

            _index++;
            outcomes[outcome] = ParseSteps(header.Indent + 2);
        }

        return new BattleStep(battleId, outcomes);
    }

    private BranchStep ParseBranch(int indent)
    {
        var cases = new List<BranchCase>();
        IReadOnlyList<Step>? fallback = null;
        var sawElse = false;
        do
        {
            var line = Peek()!;
            var condition = ReadBranchCondition(line, ref sawElse);
            _index++;
            var body = ParseSteps(indent + 2);
            if (condition is null)
            {
                fallback = body;
            }
            else
            {
                cases.Add(new BranchCase(condition, body));
            }
        } while (HasNextBranch(indent));

        return new BranchStep(cases.ToArray(), fallback);
    }

    private ParsedExpression? ReadBranchCondition(StoryLine line, ref bool sawElse)
    {
        if (sawElse)
        {
            throw Error(line, "else 必须是条件分支的最后一项。");
        }
        if (line.IsKeyword("else"))
        {
            EnsureNoArgument(line, "else");
            sawElse = true;
            return null;
        }

        var keyword = line.IsKeyword("if") ? "if" : "elif";
        var condition = ReadArgument(line, keyword);
        return ParseExpression(line, condition, line.Text.Length - condition.Length);
    }

    private bool HasNextBranch(int indent) =>
        Peek() is { } next && next.Indent == indent && (next.IsKeyword("elif") || next.IsKeyword("else"));

    private ParsedExpression ParseExpression(StoryLine line, string source, int columnOffset)
    {
        try
        {
            return _expressions.ParseExpression(source, ExpressionSourceName(line, columnOffset));
        }
        catch (ExpressionParseException exception)
        {
            throw ExpressionError(line, columnOffset, exception);
        }
    }

    private ParsedCall ParseCall(StoryLine line)
    {
        try
        {
            return _expressions.ParseCall(line.Text, ExpressionSourceName(line, 0));
        }
        catch (ExpressionParseException exception)
        {
            throw ExpressionError(line, 0, exception);
        }
    }

    private string ExpressionSourceName(StoryLine line, int columnOffset) =>
        $"{_sourceName}({line.Number},{line.Indent + columnOffset + 1}) [剧情段 '{_segmentName}']";

    private StoryCompilationException ExpressionError(StoryLine line, int columnOffset, ExpressionParseException exception) =>
        new(_sourceName, _segmentName, line.Number, line.Indent + columnOffset + exception.Span.Column,
            exception.Message, exception);

    private void ValidateVariable(StoryLine line, string target)
    {
        try
        {
            ExpressionSymbol.Validate(target);
        }
        catch (ArgumentException exception)
        {
            throw Error(line, $"无效的变量标识符 '{target}'。", exception);
        }
    }

    private string ReadArgument(StoryLine line, string keyword)
    {
        var argument = line.Text[keyword.Length..].Trim();
        if (argument.Length == 0)
        {
            throw Error(line, $"{keyword} 之后必须提供参数。");
        }
        return argument;
    }

    private void EnsureNoArgument(StoryLine line, string keyword)
    {
        if (line.Text.Length != keyword.Length)
        {
            throw Error(line, $"{keyword} 后不能跟参数。");
        }
    }

    private StoryLine? Peek(int ahead = 0) =>
        _index + ahead < _lines.Count ? _lines[_index + ahead] : null;

    private StoryCompilationException Error(StoryLine line, string message, Exception? innerException = null) =>
        new(_sourceName, _segmentName, line.Number, line.Indent + 1, message, innerException);
}
