namespace JyGame
{
	public interface ILogger
	{
		void Log(string msg);

		void LogError(string msg);

		void LogWarning(string msg);
	}
}
