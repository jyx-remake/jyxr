using System;
using System.IO;
using UnityEngine;

namespace JyGame
{
	public class FileLogger : ILogger
	{
		public static FileLogger instance = new FileLogger();

		public void log(string msg)
		{
			Log(msg);
		}

		public void Log(string msg)
		{
			if (!Application.isMobilePlatform)
			{
				using (StreamWriter streamWriter = new StreamWriter(File.Open("log.txt", FileMode.Append)))
				{
					streamWriter.WriteLine(DateTime.Now.ToString() + " " + msg);
				}
			}
		}

		public void LogWarning(string msg)
		{
			if (!Application.isMobilePlatform)
			{
				using (StreamWriter streamWriter = new StreamWriter(File.Open("log.txt", FileMode.Append)))
				{
					streamWriter.WriteLine(DateTime.Now.ToString() + " [warning]" + msg);
				}
			}
		}

		public void LogError(string msg)
		{
			if (!Application.isMobilePlatform)
			{
				using (StreamWriter streamWriter = new StreamWriter(File.Open("log.txt", FileMode.Append)))
				{
					streamWriter.WriteLine(DateTime.Now.ToString() + " [error]" + msg);
				}
			}
		}
	}
}
