using System;

namespace DnsClient.Logging;

/// <summary>
/// Interface used for logging internal errors of the DNS client
/// </summary>
public interface IErrorLogging
{
	/// <summary>
	/// Called when an error (that is NOT an exception) needs to be logged
	/// </summary>
	/// <param name="message">Error content to log</param>
	void LogError(string message);

	/// <summary>
	/// Called when an error (that is an exception) needs to be logged
	/// </summary>
	/// <param name="message">Error content to log</param>
	/// <param name="e">Exception to log</param>
	void LogException(string message, Exception e);
}