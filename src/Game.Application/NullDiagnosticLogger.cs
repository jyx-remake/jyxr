namespace Game.Application;

public sealed class NullDiagnosticLogger : IDiagnosticLogger
{
    public static NullDiagnosticLogger Instance { get; } = new();

    private NullDiagnosticLogger()
    {
    }

    public void Log(DiagnosticLogLevel level, string message, Exception? exception = null)
    {
    }
}
