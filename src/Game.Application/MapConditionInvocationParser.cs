using Game.Core.Definitions;
using Game.Core.Story;

namespace Game.Application;

internal sealed class MapConditionInvocationParser
{
    public PredicateInvocation Parse(MapEventConditionDefinition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentException.ThrowIfNullOrWhiteSpace(condition.Type);

        return condition.Type switch
        {
            "always" => CreateInvocation("always"),
            "silver_at_least" => CreateInvocation("silver_at_least", ExprValue.FromNumber(ParseNonNegativeInt(condition.Value, condition.Type))),
            "gold_at_least" => CreateInvocation("gold_at_least", ExprValue.FromNumber(ParseNonNegativeInt(condition.Value, condition.Type))),
            "friendCount" => CreateInvocation("friendCount", ExprValue.FromNumber(ParseNonNegativeInt(condition.Value, condition.Type))),
            "current_map" => CreateInvocation("current_map", ExprValue.FromString(condition.Value)),
            "event_completed" => CreateInvocation("event_completed", ExprValue.FromString(condition.Value)),
            "event_finished" => CreateInvocation("event_finished", ExprValue.FromString(condition.Value)),
            "event_not_completed" => CreateInvocation("event_not_completed", ExprValue.FromString(condition.Value)),
            "event_not_finished" => CreateInvocation("event_not_finished", ExprValue.FromString(condition.Value)),
            "time_slot" => CreateMultiStringInvocation("time_slot", condition.Value),
            "in_time" => CreateMultiStringInvocation("in_time", condition.Value),
            "not_in_time" => CreateMultiStringInvocation("not_in_time", condition.Value),
            "key_in_team" => CreateInvocation("key_in_team", ExprValue.FromString(condition.Value)),
            "key_not_in_team" => CreateInvocation("key_not_in_team", ExprValue.FromString(condition.Value)),
            "in_team" => CreateInvocation("in_team", ExprValue.FromString(condition.Value)),
            "not_in_team" => CreateInvocation("not_in_team", ExprValue.FromString(condition.Value)),
            "have_item" => CreateItemInvocation("have_item", condition.Value),
            "not_have_item" or "not_has_time_key" => CreateItemInvocation("not_have_item", condition.Value),
            "level_greater_than" => CreateCharacterThresholdInvocation("level_greater_than", condition.Value, condition.Type),
            "level_less_than" => CreateCharacterThresholdInvocation("character_level_less_than", condition.Value, condition.Type),
            "shenfa_greater_than" => CreateCharacterThresholdInvocation("shenfa_greater_than", condition.Value, condition.Type),
            "skill_less_than" => CreateCharacterSkillInvocation("character_skill_less_than", condition.Value, condition.Type),
            "skill_more_than" => CreateCharacterSkillInvocation("character_skill_more_than", condition.Value, condition.Type),
            "should_finish" => CreateInvocation("should_finish", ExprValue.FromString(condition.Value)),
            "follow_story" => CreateInvocation("follow_story", ExprValue.FromString(condition.Value)),
            "should_not_finish" => CreateInvocation("should_not_finish", ExprValue.FromString(condition.Value)),
            "exceed_day" => CreateInvocation("exceed_day", ExprValue.FromNumber(ParseNonNegativeInt(condition.Value, condition.Type))),
            "not_exceed_day" => CreateInvocation("not_exceed_day", ExprValue.FromNumber(ParseNonNegativeInt(condition.Value, condition.Type))),
            "in_round" => CreateInvocation("in_round", ExprValue.FromNumber(ParseNonNegativeInt(condition.Value, condition.Type))),
            "not_in_round" => CreateInvocation("not_in_round", ExprValue.FromNumber(ParseNonNegativeInt(condition.Value, condition.Type))),
            "zhoumu_greater_than" => CreateInvocation("zhoumu_greater_than", ExprValue.FromNumber(ParseNonNegativeInt(condition.Value, condition.Type))),
            "game_mode" => CreateInvocation("game_mode", ExprValue.FromString(condition.Value)),
            "in_menpai" => CreateInvocation("in_menpai", ExprValue.FromString(condition.Value)),
            "in_sect" => CreateInvocation("in_sect", ExprValue.FromString(condition.Value)),
            "not_in_menpai" => CreateInvocation("not_in_menpai", ExprValue.FromString(condition.Value)),
            "not_in_sect" => CreateInvocation("not_in_sect", ExprValue.FromString(condition.Value)),
            "in_newbie_task" => CreateInvocation("in_newbie_task"),
            _ => throw new InvalidOperationException($"Unsupported map condition type: {condition.Type}"),
        };
    }

    private static PredicateInvocation CreateItemInvocation(string name, string value)
    {
        var parts = SplitValue(value);
        if (parts.Length == 0)
        {
            throw new InvalidOperationException($"Map condition '{name}' requires at least an item id.");
        }

        return parts.Length >= 2
            ? CreateInvocation(name, ExprValue.FromString(parts[0]), ExprValue.FromNumber(ParseNonNegativeInt(parts[1], name)))
            : CreateInvocation(name, ExprValue.FromString(parts[0]));
    }

    private static PredicateInvocation CreateCharacterThresholdInvocation(string name, string value, string conditionType)
    {
        var parts = SplitValue(value);
        if (parts.Length < 2)
        {
            throw new InvalidOperationException($"Invalid value '{value}' for map condition '{conditionType}'.");
        }

        return CreateInvocation(
            name,
            ExprValue.FromString(parts[0]),
            ExprValue.FromNumber(ParseNonNegativeInt(parts[1], conditionType)));
    }

    private static PredicateInvocation CreateCharacterSkillInvocation(string name, string value, string conditionType)
    {
        var parts = SplitValue(value);
        if (parts.Length < 3)
        {
            throw new InvalidOperationException($"Invalid value '{value}' for map condition '{conditionType}'.");
        }

        return CreateInvocation(
            name,
            ExprValue.FromString(parts[0]),
            ExprValue.FromString(parts[1]),
            ExprValue.FromNumber(ParseNonNegativeInt(parts[2], conditionType)));
    }

    private static PredicateInvocation CreateMultiStringInvocation(string name, string value)
    {
        var parts = SplitValue(value);
        if (parts.Length == 0)
        {
            throw new InvalidOperationException($"Map condition '{name}' requires at least one value.");
        }

        return CreateInvocation(name, parts.Select(ExprValue.FromString).ToArray());
    }

    private static PredicateInvocation CreateInvocation(string name, params ExprValue[] arguments) =>
        new(name, arguments);

    private static int ParseNonNegativeInt(string value, string conditionType)
    {
        if (!int.TryParse(value, out var result) || result < 0)
        {
            throw new InvalidOperationException($"Invalid value '{value}' for map condition '{conditionType}'.");
        }

        return result;
    }

    private static string[] SplitValue(string value) =>
        value.Split('#', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
