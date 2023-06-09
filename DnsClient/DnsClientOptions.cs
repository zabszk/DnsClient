using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using DnsClient.Logging;

namespace DnsClient
{
	/// <summary>
	/// <see cref="DnsClient"/> options
	/// </summary>
	[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
	[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
	public struct DnsClientOptions
	{
		/// <summary>
		/// Maximum amount of attempts of performing a DNS query
		/// </summary>
		/// <exception cref="ArgumentException">Value is not greater than 0</exception>
		public ushort MaxAttempts
		{
			get => _maxAttempts;

			set
			{
				if (value == 0)
					throw new ArgumentException("maxAttempts must be greater than 0.", nameof(value));

				_maxAttempts = value;
			}
		}

		/// <summary>
		/// Controls how often the client checks for replies to each query (delay time in milliseconds)
		/// </summary>
		/// <exception cref="ArgumentException"></exception>
		public int TimeoutInnerDelay
		{
			get => _timeoutInnerDelay;

			set
			{
				if (value <= 0)
					throw new ArgumentException("maxAttempts must be greater than 0.", nameof(value));

				_timeoutInnerDelay = value;
			}
		}

		/// <summary>
		/// DNS query timeout (in milliseconds)
		/// </summary>
		public uint Timeout = 500;

		/// <summary>
		/// Errors handler
		/// </summary>
		public IErrorLogging? ErrorLogging = null;

		/// <summary>
		/// Indicates whether queries with truncated response should be retried using TCP instead of UDP
		/// </summary>
		// ReSharper disable once InconsistentNaming
		public bool UseTCPForTruncated = true;

		/// <summary>
		/// Allows to set a different endpoint for TCP connections.
		/// Ignored if <see cref="UseTCPForTruncated"/> is set to <value>FALSE</value>
		/// </summary>
		// ReSharper disable once InconsistentNaming
		public EndPoint? TCPEndpointOverride = null;

		private int _timeoutInnerDelay = 50;
		private ushort _maxAttempts = 5;

		/// <summary>
		/// Constructor
		/// </summary>
		public DnsClientOptions() { }
	}
}