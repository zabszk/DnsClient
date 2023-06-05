using System;

namespace DnsClient
{
	public class DnsClientOptions
	{
		public readonly ushort MaxAtempts;
		public readonly uint Timeout;

		public DnsClientOptions(ushort maxAttempts = 5, uint timeout = 250)
		{
			if (maxAttempts == 0)
				throw new ArgumentException("maxAttempts must be greater than 0.", nameof(maxAttempts));

			MaxAtempts = maxAttempts;
			Timeout = timeout;
		}
	}
}