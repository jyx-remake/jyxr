using System.Text.Json;

namespace Game.Core.Story;

internal sealed class StoryScriptJsonParser(JsonElement root)
{
    public StoryScript Parse()
    {
        EnsureObject(root, "root");
        var version = GetRequiredInt32(root, "version");
        var segmentsElement = GetRequiredProperty(root, "segments");
        EnsureArray(segmentsElement, "segments");

        var segments = new List<Segment>();
        foreach (var segmentElement in segmentsElement.EnumerateArray())
        {
            segments.Add(ParseSegment(segmentElement));
        }

        return new StoryScript(version, segments);
    }

    private Segment ParseSegment(JsonElement element)
    {
        EnsureObject(element, "segment");
        var name = GetRequiredString(element, "name");
        var steps = ParseSteps(GetRequiredProperty(element, "steps"), "segment.steps");
        return new Segment(name, steps);
    }

    private IReadOnlyList<Step> ParseSteps(JsonElement element, string path)
    {
        EnsureArray(element, path);
        var steps = new List<Step>();
        foreach (var stepElement in element.EnumerateArray())
        {
            steps.Add(ParseStep(stepElement));
        }

        return steps;
    }

    private Step ParseStep(JsonElement element)
    {
        EnsureObject(element, "step");
        var kind = GetRequiredString(element, "kind");
        return kind switch
        {
            "dialogue" => new DialogueStep(
                GetRequiredString(element, "speaker"),
                GetRequiredString(element, "text")),
            "command" => new CommandStep(
                GetRequiredString(element, "name"),
                ParseValueArgs(GetRequiredProperty(element, "args"), "command.args")),
            "jump" => new JumpStep(GetRequiredString(element, "target")),
            "choice" => ParseChoiceStep(element),
            "battle" => ParseBattleStep(element),
            "branch" => ParseBranchStep(element),
            _ => throw new StoryRuntimeException($"Unsupported step kind '{kind}'."),
        };
    }

    private ChoiceStep ParseChoiceStep(JsonElement element)
    {
        var promptElement = GetRequiredProperty(element, "prompt");
        EnsureObject(promptElement, "choice.prompt");
        var prompt = new ChoicePrompt(
            GetRequiredString(promptElement, "speaker"),
            GetRequiredString(promptElement, "text"));

        var optionsElement = GetRequiredProperty(element, "options");
        EnsureArray(optionsElement, "choice.options");
        var options = new List<ChoiceOption>();
        foreach (var optionElement in optionsElement.EnumerateArray())
        {
            EnsureObject(optionElement, "choice.option");
            options.Add(new ChoiceOption(
                GetRequiredString(optionElement, "text"),
                ParseSteps(GetRequiredProperty(optionElement, "steps"), "choice.option.steps")));
        }

        return new ChoiceStep(prompt, options);
    }

    private BattleStep ParseBattleStep(JsonElement element)
    {
        var battleId = GetRequiredString(element, "battleId");
        var outcomesElement = GetRequiredProperty(element, "outcomes");
        EnsureObject(outcomesElement, "battle.outcomes");

        var outcomes = new Dictionary<BattleOutcome, IReadOnlyList<Step>>();
        foreach (var property in outcomesElement.EnumerateObject())
        {
            outcomes.Add(ParseBattleOutcome(property.Name), ParseSteps(property.Value, $"battle.outcomes.{property.Name}"));
        }

        return new BattleStep(battleId, outcomes);
    }

    private BranchStep ParseBranchStep(JsonElement element)
    {
        var casesElement = GetRequiredProperty(element, "cases");
        EnsureArray(casesElement, "branch.cases");
        var cases = new List<BranchCase>();
        foreach (var caseElement in casesElement.EnumerateArray())
        {
            EnsureObject(caseElement, "branch.case");
            cases.Add(new BranchCase(
                ParseExpression(GetRequiredProperty(caseElement, "when")),
                ParseSteps(GetRequiredProperty(caseElement, "steps"), "branch.case.steps")));
        }

        IReadOnlyList<Step>? fallback = null;
        if (TryGetProperty(element, "fallback", out var fallbackElement) && fallbackElement.ValueKind != JsonValueKind.Null)
        {
            fallback = ParseSteps(fallbackElement, "branch.fallback");
        }

        return new BranchStep(cases, fallback);
    }

    private ExprNode ParseExpression(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => ParseLiteralExpression(element),
            JsonValueKind.Array => ParseArrayExpression(element),
            _ => throw new StoryRuntimeException($"Unsupported expression token kind '{element.ValueKind}'."),
        };
    }

    private ExprNode ParseArrayExpression(JsonElement element)
    {
        var items = element.EnumerateArray().ToArray();
        if (items.Length == 0)
        {
            throw new StoryRuntimeException("Expression array cannot be empty.");
        }

        if (items[0].ValueKind != JsonValueKind.String)
        {
            throw new StoryRuntimeException("Expression operator must be a string.");
        }

        var op = items[0].GetString() ?? string.Empty;
        return op switch
        {
            "var" => ParseVariableExpression(items),
            "pred" => ParsePredicateExpression(items),
            "not" => ParseNotExpression(items),
            "and" or "or" or "==" or "!=" or ">" or ">=" or "<" or "<=" => ParseBinaryExpression(op, items),
            _ => throw new StoryRuntimeException($"Unsupported expression operator '{op}'."),
        };
    }

    private static ExprNode ParseVariableExpression(IReadOnlyList<JsonElement> items)
    {
        if (items.Count != 2 || items[1].ValueKind != JsonValueKind.String)
        {
            throw new StoryRuntimeException("Variable expression must be ['var', <name>].");
        }

        return new VariableExprNode(items[1].GetString() ?? string.Empty);
    }

    private ExprNode ParsePredicateExpression(IReadOnlyList<JsonElement> items)
    {
        if (items.Count < 2 || items[1].ValueKind != JsonValueKind.String)
        {
            throw new StoryRuntimeException("Predicate expression must start with ['pred', <name>, ...].");
        }

        var args = new List<ExprNode>();
        for (var index = 2; index < items.Count; index += 1)
        {
            args.Add(ParseValueArg(items[index], "pred args"));
        }

        return new PredicateExprNode(items[1].GetString() ?? string.Empty, args);
    }

    private ExprNode ParseNotExpression(IReadOnlyList<JsonElement> items)
    {
        if (items.Count != 2)
        {
            throw new StoryRuntimeException("Not expression must be ['not', <expr>].");
        }

        return new NotExprNode(ParseExpression(items[1]));
    }

    private ExprNode ParseBinaryExpression(string op, IReadOnlyList<JsonElement> items)
    {
        if (items.Count != 3)
        {
            throw new StoryRuntimeException($"Binary expression '{op}' must be ['{op}', <left>, <right>].");
        }

        return new BinaryExprNode(
            ParseBinaryOperator(op),
            ParseExpression(items[1]),
            ParseExpression(items[2]));
    }

    private static ExprBinaryOperator ParseBinaryOperator(string op) => op switch
    {
        "and" => ExprBinaryOperator.And,
        "or" => ExprBinaryOperator.Or,
        "==" => ExprBinaryOperator.Equal,
        "!=" => ExprBinaryOperator.NotEqual,
        ">" => ExprBinaryOperator.GreaterThan,
        ">=" => ExprBinaryOperator.GreaterThanOrEqual,
        "<" => ExprBinaryOperator.LessThan,
        "<=" => ExprBinaryOperator.LessThanOrEqual,
        _ => throw new StoryRuntimeException($"Unsupported binary operator '{op}'."),
    };

    private static BattleOutcome ParseBattleOutcome(string raw) => raw switch
    {
        "win" => BattleOutcome.Win,
        "lose" => BattleOutcome.Lose,
        "timeout" => BattleOutcome.Timeout,
        _ => throw new StoryRuntimeException($"Unsupported battle outcome '{raw}'."),
    };

    private IReadOnlyList<ExprNode> ParseValueArgs(JsonElement element, string path)
    {
        EnsureArray(element, path);
        var values = new List<ExprNode>();
        foreach (var item in element.EnumerateArray())
        {
            values.Add(ParseValueArg(item, path));
        }

        return values;
    }

    private ExprNode ParseValueArg(JsonElement element, string path)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String or JsonValueKind.Number => ParseLiteralExpression(element),
            JsonValueKind.Array => ParseVariableValueArg(element, path),
            _ => throw new StoryRuntimeException($"{path} must contain only strings, numbers, or ['var', name]."),
        };
    }

    private static ExprNode ParseLiteralExpression(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => new LiteralExprNode(ExprValue.FromString(element.GetString() ?? string.Empty)),
            JsonValueKind.Number => new LiteralExprNode(ExprValue.FromNumber(element.GetDouble())),
            JsonValueKind.True => new LiteralExprNode(ExprValue.FromBoolean(true)),
            JsonValueKind.False => new LiteralExprNode(ExprValue.FromBoolean(false)),
            _ => throw new StoryRuntimeException($"Unsupported literal token kind '{element.ValueKind}'."),
        };
    }

    private ExprNode ParseVariableValueArg(JsonElement element, string path)
    {
        var value = ParseArrayExpression(element);
        if (value is VariableExprNode)
        {
            return value;
        }

        throw new StoryRuntimeException($"{path} must contain only variable references in array form.");
    }

    private static JsonElement GetRequiredProperty(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value))
        {
            throw new StoryRuntimeException($"Missing required property '{name}'.");
        }

        return value;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals(name))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string GetRequiredString(JsonElement element, string name)
    {
        var value = GetRequiredProperty(element, name);
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new StoryRuntimeException($"Property '{name}' must be a string.");
        }

        return value.GetString() ?? string.Empty;
    }

    private static int GetRequiredInt32(JsonElement element, string name)
    {
        var value = GetRequiredProperty(element, name);
        if (!value.TryGetInt32(out var result))
        {
            throw new StoryRuntimeException($"Property '{name}' must be an integer.");
        }

        return result;
    }

    private static void EnsureObject(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new StoryRuntimeException($"{path} must be a JSON object.");
        }
    }

    private static void EnsureArray(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new StoryRuntimeException($"{path} must be a JSON array.");
        }
    }
}
