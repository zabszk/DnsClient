using System;

namespace DnsClient.Logging;

public class StdoutLogger : IErrorLogging
{
	public void LogError(string message) => Console.WriteLine($"[DNS CLIENT] Error: {message}");

	public void LogException(string message, Exception e) => Console.WriteLine($"[DNS CLIENT] Error: {message}{Environment.NewLine}Exception: {e.Message}{Environment.NewLine}{e.StackTrace}");
}