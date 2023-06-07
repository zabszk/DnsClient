using System;

namespace DnsClient.Logging;

/// <summary>
/// Logs all errors to stdout (standard output)
/// </summary>
public class StdoutLogger : IErrorLogging
{
	/// <inheritdoc />
	public void LogError(string message) => Console.WriteLine($"[DNS CLIENT] Error: {message}");

	/// <inheritdoc />
	public void LogException(string message, Exception e) => Console.WriteLine($"[DNS CLIENT] Error: {message}{Environment.NewLine}Exception: {e.GetType()} - {e.Message}{Environment.NewLine}{e.StackTrace}");
}