using System;

namespace DnsClient.Logging;

public interface IErrorLogging
{
	void LogError(string message);
	void LogException(string message, Exception e);
}