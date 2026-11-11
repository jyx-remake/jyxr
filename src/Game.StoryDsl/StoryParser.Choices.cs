using Game.Core.Story;
using Game.Expressions;

namespace Game.StoryDsl;

internal sealed partial class StoryParser
{
    private Step ParseDialogue(StoryLine line, int separator)
    {
        var speaker = line.Text[..separator].Trim();
        var text = line.Text[(separator + 1)..].Trim();
        string? style = null;
        if (text.StartsWith("[#", StringComparison.Ordinal))
        {
            var closing = text.IndexOf(']');
            if (closing < 0)
            {
                throw Error(line, "展示样式标签缺少右方括号 ']'。");
            }
            var tag = text[2..closing].Split('=', 2, StringSplitOptions.TrimEntries);
            if (tag.Length != 2 || tag[0] != "style" || tag[1].Length == 0 ||
                tag[1].Any(character => !char.IsLetterOrDigit(character) && character is not ('_' or '.' or '-')))
            {
                throw Error(line, "展示标签必须使用 '[#style=样式ID]' 格式。");
            }
            style = tag[1];
            text = text[(closing + 1)..].Trim();
        }
        if (text.Contains("[#", StringComparison.Ordinal))
        {
            throw Error(line, "同一条语句只能在正文开头配置一个 style 标签。");
        }

        if (!IsChoiceStart(line.Indent))
        {
            if (style is not null)
            {
                throw Error(line, "style 只能配置在 choice 的提示对白上。");
            }
            return new DialogueStep(speaker, text);
        }

        if (style is not (null or "regular" or "bold"))
        {
            throw Error(line, $"不支持的选项样式 '{style}'，可使用 regular 或 bold。");
        }

        var blocks = new List<ChoiceBlock>();
        while (IsChoiceStart(line.Indent))
        {
            blocks.Add(Peek()!.IsOption
                ? new ChoiceOptionsBlock(ParseOptions(line.Indent))
                : ParseChoiceBranch(line.Indent));
        }

        return new ChoiceStep(new ChoicePrompt(speaker, text), blocks.ToArray(),
            style == "bold" ? ChoiceStyle.Bold : ChoiceStyle.Regular);
    }

    private bool IsChoiceStart(int indent) =>
        Peek() is { } line && line.Indent == indent &&
        (line.IsOption || line.IsKeyword("if") && Peek(1) is { } body &&
         body.Indent == indent + 2 && body.IsOption);

    private IReadOnlyList<ChoiceOption> ParseOptions(int indent)
    {
        var options = new List<ChoiceOption>();
        while (Peek() is { } line && line.Indent == indent && line.IsOption)
        {
            var text = line.Text[1..].TrimStart();
            var delimiter = FindConditionSuffix(text);
            ParsedExpression? condition = null;
            if (delimiter >= 0)
            {
                var conditionSource = text[(delimiter + 2)..].Trim();
                condition = ParseExpression(line, conditionSource, line.Text.Length - conditionSource.Length);
                text = text[..delimiter].TrimEnd();
            }
            if (text.Contains("[#", StringComparison.Ordinal))
            {
                throw Error(line, "选项暂不支持展示标签，请在 choice 的提示对白上配置 style。");
            }

            _index++;
            options.Add(new ChoiceOption(text, condition, ParseSteps(indent + 2)));
        }
        return options.ToArray();
    }

    private ChoiceBranchBlock ParseChoiceBranch(int indent)
    {
        var cases = new List<ChoiceBranchCase>();
        IReadOnlyList<ChoiceOption>? fallback = null;
        var sawElse = false;
        do
        {
            var line = Peek()!;
            var condition = ReadBranchCondition(line, ref sawElse);
            _index++;
            var options = ParseOptions(indent + 2);
            if (options.Count == 0)
            {
                throw Error(line, "choice 条件分支至少需要一个缩进的 '- 选项'。");
            }
            if (condition is null)
            {
                fallback = options;
            }
            else
            {
                cases.Add(new ChoiceBranchCase(condition, options));
            }
        } while (HasNextBranch(indent));

        return new ChoiceBranchBlock(cases.ToArray(), fallback);
    }

    private static int FindConditionSuffix(string text)
    {
        var quote = '\0';
        var escaped = false;
        var brackets = 0;
        var result = -1;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (escaped)
            {
                escaped = false;
                continue;
            }
            if (quote != '\0')
            {
                if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == quote)
                {
                    quote = '\0';
                }
                continue;
            }
            if (character is '\'' or '"')
            {
                quote = character;
            }
            else if (character == '[')
            {
                brackets++;
            }
            else if (character == ']' && brackets > 0)
            {
                brackets--;
            }
            else if (brackets == 0 && character == 'i' && index + 1 < text.Length && text[index + 1] == 'f' &&
                     index > 0 && char.IsWhiteSpace(text[index - 1]) &&
                     (index + 2 == text.Length || char.IsWhiteSpace(text[index + 2])))
            {
                result = index;
                index++;
            }
        }
        return result;
    }
}
