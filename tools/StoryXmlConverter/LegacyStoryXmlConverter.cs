using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Game.StoryDsl;

namespace Game.Tools.StoryXml;

public static partial class LegacyStoryXmlConverter
{
    public static string Convert(string xml, string sourceName = "story XML")
    {
        ArgumentNullException.ThrowIfNull(xml);
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            });
            var document = XDocument.Load(reader, LoadOptions.SetLineInfo);
            return new Conversion(sourceName).Convert(document.Root!);
        }
        catch (XmlException exception)
        {
            throw new FormatException(
                $"{sourceName}({exception.LineNumber},{exception.LinePosition}): {exception.Message}", exception);
        }
    }

    private sealed partial class Conversion(string sourceName)
    {
        public string Convert(XElement root)
        {
            if (root.Name != "root")
            {
                throw Error(root, "剧情 XML 必须使用 <root> 根元素。");
            }

            var lines = new List<string>();
            foreach (var story in root.Elements("story"))
            {
                if (lines.Count > 0)
                {
                    lines.Add("");
                }
                lines.Add("# " + RequiredAttribute(story, "name"));
                EmitStory(story, lines);
            }

            var source = string.Join('\n', lines).TrimEnd() + "\n";
            StoryCompiler.Compile(source, sourceName + " (converted)");
            return source;
        }

        private void EmitStory(XElement story, List<string> lines)
        {
            var results = story.Elements("result").ToArray();
            var consumed = new HashSet<XElement>();
            foreach (var action in story.Elements("action"))
            {
                var type = TypeParts(action)[0];
                if (type is "select" or "battle")
                {
                    var matches = results.Where(result => !consumed.Contains(result) &&
                        (type == "select" ? IsSelectResult(result) : BattleOutcome(Value(result, "ret")) is not null)).ToArray();
                    consumed.UnionWith(matches);
                    if (type == "select")
                    {
                        EmitSelect(action, matches, lines);
                    }
                    else
                    {
                        EmitBattle(action, matches, lines);
                    }
                    continue;
                }

                if (type == "dialog")
                {
                    var (speaker, text) = SplitFirst(Value(action), '#');
                    lines.Add($"{speaker}：{ConvertText(text)}");
                }
                else if (ConvertAction(action) is { } statement)
                {
                    lines.Add(statement);
                }
            }
            EmitResults(results.Where(result => !consumed.Contains(result)), 0, lines);
        }

        private void EmitSelect(XElement action, IReadOnlyList<XElement> results, List<string> lines)
        {
            var (speaker, remainder) = SplitFirst(Value(action), '#');
            var parts = remainder.Split('#', StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                throw Error(action, "SELECT 至少需要一个选项。");
            }
            lines.Add($"{speaker}：{ConvertText(parts[0])}");
            var grouped = new Dictionary<int, List<XElement>>();
            var extras = new List<XElement>();
            foreach (var result in results)
            {
                if (int.TryParse(Value(result, "ret"), NumberStyles.None, CultureInfo.InvariantCulture, out var index) &&
                    index < parts.Length - 1)
                {
                    if (!grouped.TryGetValue(index, out var group))
                    {
                        group = [];
                        grouped.Add(index, group);
                    }
                    group.Add(result);
                }
                else
                {
                    extras.Add(result);
                }
            }

            for (var index = 1; index < parts.Length; index++)
            {
                lines.Add("- " + ConvertText(parts[index]));
                EmitResults(grouped.GetValueOrDefault(index - 1) ?? [], 1, lines);
            }
            foreach (var result in extras)
            {
                lines.Add("- " + ConvertText(Value(result, "ret").Trim()));
                EmitResult(result, 1, lines, commented: false);
            }
        }

        private void EmitBattle(XElement action, IReadOnlyList<XElement> results, List<string> lines)
        {
            lines.Add("battle " + Value(action).Trim());
            foreach (var outcome in new[] { "win", "lose", "timeout" })
            {
                var matching = results.Where(result => BattleOutcome(Value(result, "ret")) == outcome).ToArray();
                if (matching.Length == 0)
                {
                    continue;
                }
                lines.Add("- " + outcome);
                EmitResults(matching, 1, lines);
            }
        }

        private void EmitResults(IEnumerable<XElement> input, int indent, List<string> lines)
        {
            var results = input.Where(result => !RemovedCommands.Contains(TypeParts(result)[0])).ToArray();
            var unconditional = results.Where(result => !result.Elements("condition").Any()).ToArray();
            var hasConditions = results.Any(result => result.Elements("condition").Any());
            var fallback = hasConditions ? unconditional.LastOrDefault()
                : unconditional.FirstOrDefault(result => IsNumericRet(Value(result, "ret"))) ?? unconditional.FirstOrDefault();
            foreach (var result in results)
            {
                var active = result.Elements("condition").Any() || ReferenceEquals(result, fallback);
                EmitResult(result, indent, lines, commented: !active);
            }
        }

        private void EmitResult(XElement result, int indent, List<string> lines, bool commented)
        {
            var statement = TypeParts(result)[0] == "story"
                ? "jump " + Value(result).Trim()
                : ConvertAction(result);
            if (statement is null)
            {
                return;
            }
            var condition = string.Join(" and ", result.Elements("condition").Select(ConvertCondition));
            var prefix = new string(' ', indent * 2) + (commented ? "// " : "");
            if (condition.Length == 0)
            {
                lines.Add(prefix + statement);
            }
            else
            {
                lines.Add(prefix + "if " + condition);
                lines.Add(prefix + "  " + statement);
            }
        }

        private static bool IsSelectResult(XElement result)
        {
            var ret = Value(result, "ret");
            return ret.Length > 0 && ret is not ("win" or "lose" or "timeout");
        }

        private static string? BattleOutcome(string ret) => ret switch
        {
            "win" or "0" => "win",
            "lose" or "1" => "lose",
            "timeout" or "2" => "timeout",
            _ => null,
        };

        private static bool IsNumericRet(string value) =>
            value.Length > 0 && value.All(character => character is >= '0' and <= '9');

        private string RequiredAttribute(XElement element, string name) =>
            (string?)element.Attribute(name) ?? throw Error(element, $"<{element.Name}> 缺少 {name} 属性。");

        private string[] TypeParts(XElement element)
        {
            var parts = RequiredAttribute(element, "type").ToLowerInvariant()
                .Split(['.', '…'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length > 0 ? parts : throw Error(element, "type 不能为空。");
        }

        private static string Value(XElement element, string attribute = "value") =>
            (string?)element.Attribute(attribute) ?? "";

        private static string[] Arguments(XElement element) =>
            Value(element).Split('#', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private string RequiredArgument(XElement element, string[] args, int index) =>
            index < args.Length && args[index].Length > 0 ? args[index]
                : throw Error(element, $"{Value(element, "type")} 缺少第 {index + 1} 个参数。");

        private FormatException Error(XElement element, string message)
        {
            var line = (IXmlLineInfo)element;
            return new FormatException($"{sourceName}({line.LineNumber},{line.LinePosition}): {message}");
        }

        private static (string First, string Remainder) SplitFirst(string value, char delimiter)
        {
            var index = value.IndexOf(delimiter);
            return index < 0 ? ("", value.Trim()) : (value[..index].Trim(), value[(index + 1)..].Trim());
        }

        private static string Quote(string value)
        {
            var result = new StringBuilder("'");
            foreach (var character in value)
            {
                result.Append(character switch
                {
                    '\\' => "\\\\",
                    '\'' => "\\'",
                    '\r' => "\\r",
                    '\n' => "\\n",
                    '\t' => "\\t",
                    _ => character.ToString(),
                });
            }
            return result.Append('\'').ToString();
        }

        private static string Call(string name, params string[] args) => $"{name}({string.Join(", ", args)})";

        private double ReadNumber(XElement element, string value)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number))
            {
                throw Error(element, $"{Value(element, "type")} 参数不是有效数字 '{value}'。");
            }
            return number;
        }

        private string Number(XElement element, string value) => FormatNumber(ReadNumber(element, value));

        private static string FormatNumber(double number) =>
            number == 0 ? "0" : number.ToString("R", CultureInfo.InvariantCulture);

        private string Boolean(XElement element, string value) => value.Trim().ToLowerInvariant() switch
        {
            "true" or "on" or "1" => "true",
            "false" or "off" or "0" => "false",
            _ => throw Error(element, $"参数不是 Boolean '{value}'。"),
        };

        private string List(XElement element, string value)
        {
            var text = value.Trim();
            if (!text.StartsWith('[') || !text.EndsWith(']'))
            {
                throw Error(element, $"列表参数格式错误 '{value}'。");
            }
            var items = text[1..^1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (items.Length == 0)
            {
                throw Error(element, "列表参数不能为空。");
            }
            return "[" + string.Join(", ", items.Select(Quote)) + "]";
        }
    }
}
