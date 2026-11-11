using System.Text.RegularExpressions;
using System.Xml.Linq;
using Game.Expressions;

namespace Game.Tools.StoryXml;

public static partial class LegacyStoryXmlConverter
{
    private sealed partial class Conversion
    {
        private static readonly HashSet<string> RemovedCommands = new(StringComparer.Ordinal) { "newbie", "touch" };
        private static readonly HashSet<string> ValuelessCommands = new(StringComparer.Ordinal)
        {
            "game_complete", "game_over", "huashan", "main_menu", "next_round", "restart", "tower", "trial", "refine", "zhenlong",
        };

        private static readonly Dictionary<string, string> CommandNames = new(StringComparer.Ordinal)
        {
            ["animation"] = "set_model",
            ["cost_day"] = "advance_days",
            ["cost_item"] = "remove_item",
            ["daode"] = "change_morality",
            ["effect"] = "sound",
            ["game"] = "minigame",
            ["gamefin"] = "game_complete",
            ["gameover"] = "game_over",
            ["get_money"] = "change_silver",
            ["get_exp"] = "grant_exp",
            ["get_point"] = "grant_points",
            ["grant_point"] = "grant_points",
            ["growtemplate"] = "set_growth",
            ["haogan"] = "change_favorability",
            ["item"] = "change_item",
            ["leave_follow"] = "leave_follower",
            ["levelup"] = "level_up",
            ["log"] = "journal",
            ["mainmenu"] = "main_menu",
            ["menpai"] = "set_sect",
            ["nextzhoumu"] = "next_round",
            ["nick"] = "unlock_achievement",
            ["random_item"] = "add_random_item",
            ["random_join"] = "join_random",
            ["select_head"] = "select_portrait",
            ["select_menpai"] = "select_sect",
            ["set_game_mode"] = "set_difficulty",
            ["set_map"] = "map",
            ["tutorial"] = "map",
            ["movie"] = "video",
            ["xiangzi"] = "chest",
            ["xilian"] = "refine",
            ["yuanbao"] = "change_yuanbao",
            ["zhenlongqiju"] = "zhenlong",
        };

        private static readonly Dictionary<string, int[]> NumericArguments = new(StringComparer.Ordinal)
        {
            ["advance_days"] = [0],
            ["change_favorability"] = [1],
            ["change_item"] = [1],
            ["change_morality"] = [0],
            ["change_silver"] = [0],
            ["change_stat"] = [2],
            ["change_yuanbao"] = [0],
            ["grant_exp"] = [1],
            ["grant_points"] = [1],
            ["learn_external"] = [2],
            ["learn_internal"] = [2],
            ["level_up"] = [1],
            ["maxlevel"] = [1],
            ["remove_item"] = [1],
            ["set_rank"] = [0],
            ["set_round"] = [0],
            ["set_time_key"] = [1],
            ["upgrade_external"] = [2],
            ["upgrade_internal"] = [2],
            ["shake"] = [0, 1],
            ["wait"] = [0],
            ["fade"] = [1],
            ["flash"] = [1, 2],
            ["filter"] = [1, 2],
            ["distort"] = [1, 2],
            ["tint"] = [1, 2],
            ["clear_filter"] = [0],
            ["clear_distort"] = [0],
            ["clear_tint"] = [0],
        };

        private static readonly HashSet<string> CharacterStats = new(StringComparer.Ordinal)
        {
            "拳掌", "剑法", "刀法", "奇门", "臂力", "身法", "悟性", "福缘", "根骨", "定力", "武学点",
            "quanzhang", "jianfa", "daofa", "qimen", "bili", "shenfa", "wuxing", "fuyuan", "gengu", "dingli", "wuxue",
            "maxhp", "max_hp", "maxmp", "max_mp", "attack", "defence", "evasion", "accuracy", "crit_chance",
            "crit_mult", "anti_crit_chance", "lifesteal", "anti_debuff", "speed", "movement",
        };

        private string? ConvertAction(XElement element)
        {
            var parts = TypeParts(element);
            var name = parts[0];
            if (RemovedCommands.Contains(name))
            {
                return null;
            }

            var args = Arguments(element);
            var command = CommandNames.GetValueOrDefault(name, name);
            if (ValuelessCommands.Contains(command))
            {
                return Call(command);
            }

            string Required(int index) => RequiredArgument(element, args, index);
            switch (name)
            {
                case "set_flag" when Value(element).Trim() == "NO_GLOBAL_EVENT":
                    return Call("world_triggers", "false");
                case "clear_flag" when Value(element).Trim() == "NO_GLOBAL_EVENT":
                    return Call("world_triggers", "true");
                case "set_flag":
                    return Variable(element, Required(0)) + " = true";
                case "clear_flag":
                case "remove_var":
                    return "del " + Variable(element, Required(0));
                case "set_var":
                    return Variable(element, Required(0)) + " = " + Quote(Required(1));
                case "change_var":
                    return Variable(element, Required(0)) + " += " + Number(element, Required(1));
                case "cost_money":
                    return Call("change_silver", FormatNumber(-ReadNumber(element, Required(0))));
                case "minus_maxpoints":
                    if (Required(0) != "主角" || ReadNumber(element, Required(1)) != 5)
                    {
                        throw Error(element, "minus_maxpoints 只能迁移 主角#5 的形式。");
                    }
                    return Call("scale_stats", Quote("主角"), "0.5");
                case "head":
                    return args.Length > 1 ? Call("set_portrait", Quote(Required(0)), Quote(Required(1)))
                        : Call("set_portrait", Quote("主角"), Quote(Required(0)));
                case "change_female_name":
                    return Call("input_name", Quote("女主"), Quote(Required(0)));
                case "select_head":
                    return Call("select_portrait", Quote(args.Length > 0 ? args[0] : "主角"));
                case "world_trigger":
                    return Call("world_triggers", Boolean(element, Required(0)));
                case "toast":
                case "set_no_regret":
                    return Call(command, Boolean(element, Required(0)));
                case "random_item":
                    return args.Length > 1
                        ? Call("add_random_item", List(element, Required(0)), Number(element, Required(1)))
                        : Call("add_random_item", List(element, Required(0)));
                case "random_join":
                    return Call("join_random", List(element, Required(0)));
                case "learn":
                case "remove":
                    return ConvertSkillMutation(element, name, parts, args);
                case "upgrade":
                    return ConvertUpgrade(element, parts, args);
                default:
                    var numericIndexes = NumericArguments.GetValueOrDefault(command) ?? [];
                    return Call(command, args.Select((value, index) => numericIndexes.Contains(index)
                        ? Number(element, value) : Quote(value)).ToArray());
            }
        }

        private string ConvertSkillMutation(XElement element, string operation, string[] parts, string[] args)
        {
            var category = SkillCategory(element, parts.Length > 1 ? string.Join('_', parts.Skip(1))
                : RequiredArgument(element, args, 0));
            var offset = parts.Length > 1 ? 0 : 1;
            var converted = new List<string>
            {
                Quote(RequiredArgument(element, args, offset)),
                Quote(RequiredArgument(element, args, offset + 1)),
            };
            if (operation == "learn" && category is ("external" or "internal") && args.Length > offset + 2)
            {
                converted.Add(Number(element, args[offset + 2]));
            }
            return Call($"{operation}_{category}", converted.ToArray());
        }

        private string ConvertUpgrade(XElement element, string[] parts, string[] args)
        {
            var type = (parts.Length > 1 ? string.Join('_', parts.Skip(1)) : RequiredArgument(element, args, 0)).ToLowerInvariant();
            var offset = parts.Length > 1 ? 0 : 1;
            var character = Quote(RequiredArgument(element, args, offset));
            if (CharacterStats.Contains(type))
            {
                return Call("change_stat", character, Quote(type), Number(element, RequiredArgument(element, args, offset + 1)));
            }

            var category = SkillCategory(element, type);
            if (category is not ("external" or "internal"))
            {
                throw Error(element, $"upgrade 不支持 '{type}' 分类。");
            }
            return Call($"upgrade_{category}", character, Quote(RequiredArgument(element, args, offset + 1)),
                args.Length > offset + 2 ? Number(element, args[offset + 2]) : "1");
        }

        private string SkillCategory(XElement element, string value) => value.Trim().ToLowerInvariant() switch
        {
            "skill" or "skilll" or "external" or "externalskill" or "external_skill" => "external",
            "internal" or "internalskill" or "internal_skill" => "internal",
            "special" or "specialskill" or "special_skill" => "special",
            "talent" => "talent",
            _ => throw Error(element, $"未知的武学分类 '{value}'。"),
        };

        private string Variable(XElement element, string value)
        {
            try
            {
                ExpressionSymbol.Validate(value);
                return value;
            }
            catch (ArgumentException)
            {
                throw Error(element, $"变量名不是有效标识符 '{value}'。");
            }
        }

        private string ConvertCondition(XElement element)
        {
            var type = RequiredAttribute(element, "type").Trim().ToLowerInvariant();
            var args = Arguments(element);
            string Text(int index) => Quote(RequiredArgument(element, args, index));
            string Numeric(int index) => Number(element, RequiredArgument(element, args, index));
            string Guard(string query) => args[0] == "主角" ? query : $"in_team({Text(0)}) and {query}";

            return type switch
            {
                "should_finish" => $"story_completed({Text(0)})",
                "should_not_finish" => $"not story_completed({Text(0)})",
                "follow_story" => $"last_story_is({Text(0)})",
                "have_item" or "has_item" => $"item_count({Text(0)}) >= {(args.Length > 1 ? Numeric(1) : "1")}",
                "not_have_item" => $"item_count({Text(0)}) == 0",
                "have_money" => $"silver >= {Numeric(0)}",
                "have_yuanbao" => $"yuanbao >= {Numeric(0)}",
                "in_round" => $"round == {Numeric(0)}",
                "game_mode" => $"difficulty == {Text(0)}",
                "exceed_day" => $"elapsed_days > {Numeric(0)}",
                "probability" => $"chance({Probability(element, RequiredArgument(element, args, 0))})",
                "in_team" or "key_in_team" => $"in_team({Text(0)})",
                "not_in_team" or "key_not_in_team" => $"not in_team({Text(0)})",
                "not_has_time_key" => $"not has_time_key({Text(0)})",
                "level_greater_than" => Guard($"character_level({Text(0)}) >= {Numeric(1)}"),
                "skill_less_than" => Guard($"skill_level({Text(0)}, {Text(1)}) < {Numeric(2)}"),
                "daode_more_than" => $"morality >= {Numeric(0)}",
                "daode_less_than" => $"morality < {Numeric(0)}",
                "haogan_more_than" => args.Length == 1 ? $"favorability('女主') >= {Numeric(0)}" : $"favorability({Text(0)}) >= {Numeric(1)}",
                "haogan_less_than" => args.Length == 1 ? $"favorability('女主') < {Numeric(0)}" : $"favorability({Text(0)}) < {Numeric(1)}",
                "rank" => $"rank != -1 and rank <= {Numeric(0)}",
                _ => ConvertStatCondition(element, type, args) ?? Call(type, args.Select(Quote).ToArray()),
            };
        }

        private string? ConvertStatCondition(XElement element, string type, string[] args)
        {
            var match = StatConditionPattern().Match(type);
            if (!match.Success)
            {
                return null;
            }
            var character = RequiredArgument(element, args, 0);
            var operation = match.Groups[2].Value == "less" ? "<" : ">=";
            var query = $"character_stat({Quote(character)}, {Quote(match.Groups[1].Value)}) {operation} {Number(element, RequiredArgument(element, args, 1))}";
            return character == "主角" ? query : $"in_team({Quote(character)}) and {query}";
        }

        private string Probability(XElement element, string value)
        {
            var percent = ReadNumber(element, value);
            if (percent is < 0 or > 100)
            {
                throw Error(element, $"probability 必须位于 0..100，实际为 '{value}'。");
            }
            return FormatNumber(percent / 100);
        }

        private static string ConvertText(string text) =>
            LegacyColorPattern().Replace(text, "[color=${color}]${text}[/color]")
                .Replace("\r\n", "[br]", StringComparison.Ordinal).Replace("\n", "[br]", StringComparison.Ordinal)
                .Replace("\r", "[br]", StringComparison.Ordinal).Replace('\t', ' ');

        [GeneratedRegex(@"\[\[(?<color>[A-Za-z][A-Za-z0-9_-]*):(?<text>.*?)\]\]", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex LegacyColorPattern();

        [GeneratedRegex(@"^(quanzhang|jianfa|daofa|qimen|bili|shenfa|wuxing|fuyuan|gengu|dingli)_(greater|more|less)_than$", RegexOptions.CultureInvariant)]
        private static partial Regex StatConditionPattern();
    }
}
