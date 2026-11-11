using Game.Core.Story;
using System.Text.Json.Nodes;

namespace Game.Core.Battle;

internal sealed class BattleEventScriptJsonParser(JsonNode? root, string sourceName = "story")
{
    private readonly ExpressionParser _expressionParser = new();

    public BattleEventScript Parse()
    {
        var rootObject = EnsureObject(root, "root");
        var version = GetRequiredInt32(rootObject, "version");
        if (version != BattleEventScript.CurrentVersion)
        {
            throw new BattleEventRuntimeException(
                $"Unsupported story script version '{version}'. Expected version {BattleEventScript.CurrentVersion}.");
        }

        var segments = EnsureArray(GetRequiredProperty(rootObject, "segments"), "segments");
        return new BattleEventScript(version, segments.Select(ParseBattleMainSegment).ToArray());
    }

    private BattleMainSegment ParseBattleMainSegment(JsonNode? node)
    {
        var element = EnsureObject(node, "segment");
        var name = GetRequiredString(element, "name");
        return new BattleMainSegment(name, ParseBattleSmallSegments(element, $"segment '{name}'"));
    }
    private List<BattleSmallSegment> ParseBattleSmallSegments(JsonObject segment, string path)
    {
        if (TryGetProperty(segment, "smallsegments", out var smallSegmentsNode) && smallSegmentsNode is not null)
        {
            var elements = EnsureArray(smallSegmentsNode, $"{path}.smallsegments");
            return elements.Select(element => ParseBattleSmallSegment(EnsureObject(element, $"{path}.smallsegments"))).ToList();
        }

        var steps = ParseSteps(GetRequiredProperty(segment, "steps"), $"{path}.steps");
        return [new BattleSmallSegment([new BranchCase(null, steps)], null)];
    }
    private BattleSmallSegment ParseBattleSmallSegment(JsonObject element)
    {
        var caseNodes = EnsureArray(GetRequiredProperty(element, "cases"), "battlesmallsegment.cases");
        var cases = caseNodes.Select(node =>
        {
            var caseElement = EnsureObject(node, "battlesmallsegment.case");
            ParsedExpression? when = null;
            if (TryGetProperty(caseElement, "when", out var whenNode))
            {
                if (!TryGetString(whenNode, out var whenSource))
                {
                    throw new BattleEventRuntimeException("battlesmallsegment.case.when must be a string or be omitted.");
                }

                when = ParseExpression(whenSource, "battlesmallsegment.case.when");
            }

            return new BranchCase(
                when,
                ParseSteps(GetRequiredProperty(caseElement, "steps"), "battlesmallsegment.case.steps"));
        }).ToArray();

        IReadOnlyList<BattleStep>? fallback = null;
        if (TryGetProperty(element, "fallback", out var fallbackNode) && fallbackNode is not null)
        {
            fallback = ParseSteps(fallbackNode, "battlesmallsegment.fallback");
        }

        int? maxExecutions = null;
        if (TryGetProperty(element, "maxExecutions", out var maxExecutionsNode))
        {
            if (maxExecutionsNode is not JsonValue maxExecutionsValue ||
                !maxExecutionsValue.TryGetValue<int>(out var parsedMaxExecutions) ||
                parsedMaxExecutions <= 0)
            {
                throw new BattleEventRuntimeException("battlesmallsegment.maxExecutions must be a positive integer.");
            }

            maxExecutions = parsedMaxExecutions;
        }

        return new BattleSmallSegment(cases, fallback, maxExecutions);
    }

    private IReadOnlyList<BattleStep> ParseSteps(JsonNode? node, string path)
    {
        var elements = EnsureArray(node, path);
        return elements.Select(ParseStep).ToArray();
    }

    private BattleStep ParseStep(JsonNode? node)
    {
        var element = EnsureObject(node, "step");
        var kind = GetRequiredString(element, "kind");
        return kind switch
        {
            "dialogue" => new DialogueStep(GetRequiredString(element, "speaker"), GetRequiredString(element, "text")),
            "command" => ParseCommandStep(element),
            "set" => ParseSetVariableStep(element),
            "delete" => new DeleteVariableStep(ParseVariableName(element, "target")),
            "jump" => new JumpStep(GetRequiredString(element, "target")),
            "call" => new CallStep(GetRequiredString(element, "target")),
            "return" => new ReturnStep(),
            "choice" => ParseChoiceStep(element),
            //"battle" => ParseBattleStep(element),
            //"branch" => ParseBranchStep(element),
            _ => throw new BattleEventRuntimeException($"Unsupported step kind '{kind}'."),
        };
    }

    private CommandStep ParseCommandStep(JsonObject element)
    {
        if (TryGetProperty(element, "name", out _) || TryGetProperty(element, "args", out _))
        {
            throw new BattleEventRuntimeException("Story v3 command steps only accept the string 'call' form; 'name/args' are not supported.");
        }

        return new CommandStep(ParseCall(GetRequiredString(element, "call"), "command.call"));
    }

    private SetVariableStep ParseSetVariableStep(JsonObject element) =>
        new(
            ParseVariableName(element, "target"),
            ParseExpression(GetRequiredString(element, "value"), "set.value"));

    private static string ParseVariableName(JsonObject element, string propertyName)
    {
        var name = GetRequiredString(element, propertyName);
        try
        {
            ExpressionSymbol.Validate(name);
            return name;
        }
        catch (ArgumentException exception)
        {
            throw new BattleEventRuntimeException($"Invalid story variable name '{name}'.", exception);
        }
    }

    private ChoiceStep ParseChoiceStep(JsonObject element)
    {
        var prompt = EnsureObject(GetRequiredProperty(element, "prompt"), "choice.prompt");
        var parsedPrompt = new ChoicePrompt(GetRequiredString(prompt, "speaker"), GetRequiredString(prompt, "text"));
        if (TryGetProperty(element, "groups", out _))
        {
            throw new BattleEventRuntimeException("Story v3 choice steps use 'blocks'; the old 'groups' shape is not supported.");
        }

        var blockNodes = EnsureArray(GetRequiredProperty(element, "blocks"), "choice.blocks");
        var blocks = blockNodes.Select(ParseChoiceBlock).ToArray();
        if (blocks.Length == 0)
        {
            throw new BattleEventRuntimeException("choice.blocks must contain at least one block.");
        }

        return new ChoiceStep(parsedPrompt, blocks, ParseChoiceStyle(element));
    }

    private ChoiceBlock ParseChoiceBlock(JsonNode? node)
    {
        var element = EnsureObject(node, "choice.block");
        return GetRequiredString(element, "kind") switch
        {
            "options" => new ChoiceOptionsBlock(ParseChoiceOptions(
                GetRequiredProperty(element, "options"),
                "choice.optionsBlock.options")),
            "branch" => ParseChoiceBranchBlock(element),
            var kind => throw new BattleEventRuntimeException($"Unsupported choice block kind '{kind}'."),
        };
    }

    private ChoiceBranchBlock ParseChoiceBranchBlock(JsonObject element)
    {
        var caseNodes = EnsureArray(GetRequiredProperty(element, "cases"), "choice.branch.cases");
        var cases = caseNodes.Select(node =>
        {
            var caseElement = EnsureObject(node, "choice.branch.case");
            return new ChoiceBranchCase(
                ParseExpression(GetRequiredString(caseElement, "when"), "choice.branch.case.when"),
                ParseChoiceOptions(
                    GetRequiredProperty(caseElement, "options"),
                    "choice.branch.case.options"));
        }).ToArray();
        if (cases.Length == 0)
        {
            throw new BattleEventRuntimeException("choice.branch.cases must contain at least one case.");
        }

        IReadOnlyList<ChoiceOption>? fallback = null;
        var fallbackNode = GetRequiredProperty(element, "fallback");
        if (fallbackNode is not null)
        {
            fallback = ParseChoiceOptions(fallbackNode, "choice.branch.fallback");
        }

        return new ChoiceBranchBlock(cases, fallback);
    }

    private IReadOnlyList<ChoiceOption> ParseChoiceOptions(JsonNode? node, string path)
    {
        var optionNodes = EnsureArray(node, path);
        var options = optionNodes.Select(node =>
        {
            var optionElement = EnsureObject(node, "choice.option");
            ParsedExpression? when = null;
            if (TryGetProperty(optionElement, "when", out var whenNode))
            {
                if (!TryGetString(whenNode, out var whenSource))
                {
                    throw new BattleEventRuntimeException("choice.option.when must be a string or be omitted.");
                }

                when = ParseExpression(whenSource, "choice.option.when");
            }

            return new ChoiceOption(
                GetRequiredString(optionElement, "text"),
                when,
                ParseSteps(GetRequiredProperty(optionElement, "steps"), "choice.option.steps"));
        }).ToArray();
        if (options.Length == 0)
        {
            throw new BattleEventRuntimeException($"{path} must contain at least one option.");
        }

        return options;
    }

    private static ChoiceStyle ParseChoiceStyle(JsonObject element)
    {
        if (!TryGetProperty(element, "style", out var styleNode))
        {
            return ChoiceStyle.Regular;
        }

        if (!TryGetString(styleNode, out var style))
        {
            throw new BattleEventRuntimeException("choice.style must be a string.");
        }

        return style switch
        {
            "regular" => ChoiceStyle.Regular,
            "bold" => ChoiceStyle.Bold,
            _ => throw new BattleEventRuntimeException($"Unsupported choice style '{style}'."),
        };
    }

    //private BattleStep ParseBattleStep(JsonObject element)
    //{
    //    var battleId = GetRequiredString(element, "battleId");
    //    var outcomesElement = EnsureObject(GetRequiredProperty(element, "outcomes"), "battle.outcomes");
    //    var outcomes = new Dictionary<BattleOutcome, IReadOnlyList<BattleStep>>();
    //    foreach (var property in outcomesElement)
    //    {
    //        outcomes.Add(ParseBattleOutcome(property.Key), ParseSteps(property.Value, $"battle.outcomes.{property.Key}"));
    //    }

    //    return new BattleStep(battleId, outcomes);
    //}

    //private BranchStep ParseBranchStep(JsonObject element)
    //{
    //    var caseNodes = EnsureArray(GetRequiredProperty(element, "cases"), "branch.cases");
    //    var cases = caseNodes.Select(node =>
    //    {
    //        var caseElement = EnsureObject(node, "branch.case");
    //        return new BranchCase(
    //            ParseExpression(GetRequiredString(caseElement, "when"), "branch.case.when"),
    //            ParseSteps(GetRequiredProperty(caseElement, "steps"), "branch.case.steps"));
    //    }).ToArray();

    //    IReadOnlyList<BattleStep>? fallback = null;
    //    if (TryGetProperty(element, "fallback", out var fallbackNode) && fallbackNode is not null)
    //    {
    //        fallback = ParseSteps(fallbackNode, "branch.fallback");
    //    }

    //    return new BranchStep(cases, fallback);
    //}

    private ParsedExpression ParseExpression(string source, string path)
    {
        try
        {
            return _expressionParser.ParseExpression(source, $"{sourceName}:{path}");
        }
        catch (ExpressionException exception)
        {
            throw new BattleEventRuntimeException(exception.Message, exception);
        }
    }

    private ParsedCall ParseCall(string source, string path)
    {
        try
        {
            return _expressionParser.ParseCall(source, $"{sourceName}:{path}");
        }
        catch (ExpressionException exception)
        {
            throw new BattleEventRuntimeException(exception.Message, exception);
        }
    }

    //private static BattleOutcome ParseBattleOutcome(string raw) => raw switch
    //{
    //    "win" => BattleOutcome.Win,
    //    "lose" => BattleOutcome.Lose,
    //    "timeout" => BattleOutcome.Timeout,
    //    _ => throw new BattleEventRuntimeException($"Unsupported battle outcome '{raw}'."),
    //};

    private static JsonNode? GetRequiredProperty(JsonObject element, string name)
    {
        if (!TryGetProperty(element, name, out var value))
        {
            throw new BattleEventRuntimeException($"Missing required property '{name}'.");
        }

        return value;
    }

    private static bool TryGetProperty(JsonObject element, string name, out JsonNode? value) =>
        element.TryGetPropertyValue(name, out value);

    private static string GetRequiredString(JsonObject element, string name)
    {
        var value = GetRequiredProperty(element, name);
        if (!TryGetString(value, out var result))
        {
            throw new BattleEventRuntimeException($"Property '{name}' must be a string.");
        }

        return result;
    }

    private static bool TryGetString(JsonNode? node, out string value)
    {
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var result))
        {
            value = result;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static int GetRequiredInt32(JsonObject element, string name)
    {
        var value = GetRequiredProperty(element, name);
        if (value is not JsonValue jsonValue || !jsonValue.TryGetValue<int>(out var result))
        {
            throw new BattleEventRuntimeException($"Property '{name}' must be an integer.");
        }

        return result;
    }

    private static JsonObject EnsureObject(JsonNode? node, string path)
    {
        if (node is not JsonObject result)
        {
            throw new BattleEventRuntimeException($"{path} must be a JSON object.");
        }

        return result;
    }

    private static JsonArray EnsureArray(JsonNode? node, string path)
    {
        if (node is not JsonArray result)
        {
            throw new BattleEventRuntimeException($"{path} must be a JSON array.");
        }

        return result;
    }
}
