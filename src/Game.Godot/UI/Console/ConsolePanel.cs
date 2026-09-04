using Game.Application;
using Godot;

namespace Game.Godot.UI;

public partial class ConsolePanel : JyPanel
{
	private const int MaxConsoleLineCount = 12;
	private readonly List<string> _consoleLines = [];
	private LineEdit _consoleInput = null!;
	private RichTextLabel _consoleOutput = null!;
	private Button _executeButton = null!;

	public override void _Ready()
	{
		base._Ready();

		_consoleInput = GetNode<LineEdit>("%ConsoleInput");
		_consoleOutput = GetNode<RichTextLabel>("%ConsoleOutput");
		_executeButton = GetNode<Button>("%ExecuteButton");
		_executeButton.Pressed += OnExecutePressed;
		_consoleInput.TextSubmitted += OnConsoleTextSubmitted;

		if (!Game.Config.ConsoleEnabled)
		{
			QueueFree();
			return;
		}

		AppendConsoleLine("系统", "命令行执行剧本指令，当前不支持 jump。");
		AppendConsoleLine("系统", "示例：item 道口烧鸡 / log \"踏入江湖\"");
		if (!Game.IsMobilePlatform)
		{
			_consoleInput.CallDeferred(Control.MethodName.GrabFocus);
		}
	}

	private void OnExecutePressed() => SubmitConsoleCommand(_consoleInput.Text);

	private void OnConsoleTextSubmitted(string text) => SubmitConsoleCommand(text);

	private async void SubmitConsoleCommand(string text)
	{
		var commandLine = text.Trim();
		if (string.IsNullOrWhiteSpace(commandLine))
		{
			AppendConsoleLine("控制台", "请输入有效指令。");
			return;
		}

		var closePanelsForPreview = IsFullscreenPreviewCommand(commandLine);
		if (closePanelsForPreview)
		{
			// A story jump or console-started battle is a preview request: drop
			// the console and any open panel first so the flow plays on a clean
			// screen instead of stacking under the still-open console UI.
			UIRoot.Instance.CloseMainPanel();
		}

		try
		{
			await Game.StoryService.CommandLine.ExecuteAsync(commandLine);
			if (closePanelsForPreview)
			{
				return;
			}

			_consoleInput.Clear();
			AppendConsoleLine("控制台", $"已执行剧本指令：{commandLine}");
		}
		catch (Exception exception)
		{
			Game.Logger.Error($"Console command failed: {commandLine}", exception);
			AppendConsoleLine("错误", exception.Message);
			if (closePanelsForPreview)
			{
				ReopenConsoleWithError(exception.Message);
			}
		}
	}

	private static bool IsFullscreenPreviewCommand(string commandLine)
	{
		// Matches all console forms: `story <id>`, `story('<id>')`,
		// `battle <id>` and `run_battle <id>`.
		var nameEnd = commandLine.IndexOfAny([' ', '\t', '(']);
		var name = nameEnd < 0 ? commandLine : commandLine[..nameEnd];
		return string.Equals(name, "story", StringComparison.Ordinal)
			|| string.Equals(name, "battle", StringComparison.Ordinal)
			|| string.Equals(name, "run_battle", StringComparison.Ordinal);
	}

	private static void ReopenConsoleWithError(string message)
	{
		try
		{
			if (UIRoot.Instance.ShowConsolePanel() is ConsolePanel panel)
			{
				panel.AppendErrorLine(message);
			}
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Reopening console panel failed.", exception);
		}
	}

	private void AppendErrorLine(string message) => AppendConsoleLine("错误", message);

	private void AppendConsoleLine(string source, string message)
	{
		if (!GodotObject.IsInstanceValid(this) || !IsInsideTree())
		{
			return;
		}

		_consoleLines.Add($"[color=#513523]{source}[/color]  {message}");
		while (_consoleLines.Count > MaxConsoleLineCount)
		{
			_consoleLines.RemoveAt(0);
		}

		_consoleOutput.Clear();
		foreach (var line in _consoleLines)
		{
			_consoleOutput.AppendText(line + "\n");
		}
	}
}
