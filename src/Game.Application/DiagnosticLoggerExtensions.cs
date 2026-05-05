namespace Game.Application;

public static class DiagnosticLoggerExtensions
{
	public static void Debug(this IDiagnosticLogger logger, string message) =>
		logger.Log(DiagnosticLogLevel.Debug, message);

	public static void Info(this IDiagnosticLogger logger, string message) =>
		logger.Log(DiagnosticLogLevel.Info, message);

	public static void Warning(this IDiagnosticLogger logger, string message) =>
		logger.Log(DiagnosticLogLevel.Warning, message);

	public static void Error(this IDiagnosticLogger logger, string message, Exception? exception = null) =>
		logger.Log(DiagnosticLogLevel.Error, message, exception);
}
