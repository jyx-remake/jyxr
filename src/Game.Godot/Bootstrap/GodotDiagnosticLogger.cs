using Game.Application;

namespace Game.Godot;

public sealed class GodotDiagnosticLogger : IDiagnosticLogger
{
	private readonly Action<string> _print;
	private readonly Action<string> _pushWarning;
	private readonly Action<string> _pushError;

	public GodotDiagnosticLogger(
		Action<string> print,
		Action<string>? pushWarning = null,
		Action<string>? pushError = null)
	{
		_print = print;
		_pushWarning = pushWarning ?? print;
		_pushError = pushError ?? print;
	}

	public void Log(DiagnosticLogLevel level, string message, Exception? exception = null)
	{
		var formatted = $"[{level}] {message}";
		if (exception is not null)
		{
			formatted = $"{formatted}{Environment.NewLine}{exception}";
		}

		switch (level)
		{
			case DiagnosticLogLevel.Warning:
				_pushWarning(formatted);
				break;
			case DiagnosticLogLevel.Error:
				_pushError(formatted);
				break;
			default:
				_print(formatted);
				break;
		}
	}
}
