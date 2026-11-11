namespace Game.StoryDsl;

internal sealed partial class StoryParser
{
    private List<StoryLine> ReadLines(string source)
    {
        var lines = new List<StoryLine>();
        using var reader = new StringReader(source.TrimStart('\uFEFF'));
        var number = 0;
        while (reader.ReadLine() is { } raw)
        {
            number++;
            var content = StripComment(raw).TrimEnd();
            if (content.Length == 0)
            {
                continue;
            }

            var indent = 0;
            while (indent < content.Length && content[indent] == ' ')
            {
                indent++;
            }
            var line = new StoryLine(number, indent, content[indent..]);
            var tab = raw.IndexOf('\t');
            if (tab >= 0)
            {
                throw new StoryCompilationException(_sourceName, null, number, tab + 1,
                    "禁止使用 Tab，请统一使用 2 个空格缩进一级。");
            }
            if (indent % 2 != 0)
            {
                throw Error(line, "缩进必须是 2 个空格的整数倍。");
            }
            // Bound recursive block parsing before entering the recursive descent.
            if (indent > 256)
            {
                throw Error(line, "剧情嵌套不能超过 128 层。");
            }
            lines.Add(line);
        }
        return lines;
    }

    private static string StripComment(string text)
    {
        var quote = '\0';
        var escaped = false;
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
            }
            else if (character is '\'' or '"')
            {
                quote = character;
            }
            else if (character == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                return text[..index];
            }
        }
        return text;
    }

    private static int FindDialogueSeparator(string text)
    {
        var depth = 0;
        var quote = '\0';
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quote != '\0')
            {
                if (character == '\\')
                {
                    index++;
                }
                else if (character == quote)
                {
                    quote = '\0';
                }
                continue;
            }

            if (character is '(' or '[')
            {
                depth++;
            }
            else if (character is ')' or ']')
            {
                depth = Math.Max(0, depth - 1);
            }
            else if (depth > 0 && character is '\'' or '"')
            {
                quote = character;
            }
            else if (depth == 0 && character is ':' or '：')
            {
                return index;
            }
        }
        return -1;
    }

    private sealed record StoryLine(int Number, int Indent, string Text)
    {
        public bool IsHeader => Indent == 0 && Text.StartsWith('#');
        public bool IsOption => Text.StartsWith('-');

        public bool IsKeyword(string keyword) =>
            Text == keyword || Text.StartsWith(keyword + " ", StringComparison.Ordinal);
    }
}
