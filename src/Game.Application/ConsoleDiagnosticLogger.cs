namespace Game.Application;

public sealed class ConsoleDiagnosticLogger : IDiagnosticLogger
{
    public void Log(DiagnosticLogLevel level, string message, Exception? exception = null)
    {
        var line = $"[{DateTimeOffset.Now:O}] [{level}] {message}";
        if (exception is not null)
        {
            line = $"{line}{Environment.NewLine}{exception}";
        }

        switch (level)
        {
            case DiagnosticLogLevel.Warning:
            case DiagnosticLogLevel.Error:
                Console.Error.WriteLine(line);
                break;
            default:
                Console.WriteLine(line);
                break;
        }
    }
}
