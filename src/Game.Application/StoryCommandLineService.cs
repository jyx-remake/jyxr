using System.Globalization;
using System.Text;
using Game.Core.Story;

namespace Game.Application;

public sealed class StoryCommandLineService
{
	private readonly StoryCommandDispatcher _dispatcher;

	public StoryCommandLineService(StoryCommandDispatcher dispatcher)
	{
		_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
	}

	public StoryCommandInvocation Parse(string line)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(line);

		var tokens = Tokenize(line);
		if (tokens.Count == 0)
		{
			throw new InvalidOperationException("请输入有效指令。");
		}

		var name = tokens[0];
		var args = new ExprValue[tokens.Count - 1];
		for (var index = 1; index < tokens.Count; index += 1)
		{
			args[index - 1] = ParseToken(tokens[index]);
		}

		return new StoryCommandInvocation(name, args);
	}

	public async ValueTask<StoryCommandInvocation> ExecuteAsync(
		string line,
		CancellationToken cancellationToken = default)
	{
		var invocation = Parse(line);
		await _dispatcher.ExecuteCommandAsync(invocation.Name, invocation.Arguments, cancellationToken);
		return invocation;
	}

	private static IReadOnlyList<string> Tokenize(string line)
	{
		var tokens = new List<string>();
		var current = new StringBuilder();
		var inQuotes = false;

		for (var index = 0; index < line.Length; index += 1)
		{
			var ch = line[index];
			if (ch == '"')
			{
				inQuotes = !inQuotes;
				continue;
			}

			if (char.IsWhiteSpace(ch) && !inQuotes)
			{
				FlushCurrentToken(tokens, current);
				continue;
			}

			current.Append(ch);
		}

		if (inQuotes)
		{
			throw new InvalidOperationException("命令行引号未闭合。");
		}

		FlushCurrentToken(tokens, current);
		return tokens;
	}

	private static void FlushCurrentToken(List<string> tokens, StringBuilder current)
	{
		if (current.Length == 0)
		{
			return;
		}

		tokens.Add(current.ToString());
		current.Clear();
	}

	private static ExprValue ParseToken(string token)
	{
		if (bool.TryParse(token, out var booleanValue))
		{
			return ExprValue.FromBoolean(booleanValue);
		}

		if (double.TryParse(
			token,
			NumberStyles.Float | NumberStyles.AllowLeadingSign,
			CultureInfo.InvariantCulture,
			out var numberValue))
		{
			return ExprValue.FromNumber(numberValue);
		}

		return ExprValue.FromString(token);
	}
}

public sealed record StoryCommandInvocation(
	string Name,
	IReadOnlyList<ExprValue> Arguments);
