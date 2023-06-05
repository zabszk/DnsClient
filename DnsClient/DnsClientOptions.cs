﻿using System;

namespace DnsClient
{
	public struct DnsClientOptions
	{
		public ushort MaxAtempts
		{
			get => _maxAttempts;

			set
			{
				if (value == 0)
					throw new ArgumentException("maxAttempts must be greater than 0.", nameof(value));

				_maxAttempts = value;
			}
		}

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

		public uint Timeout = 250;

		private int _timeoutInnerDelay = 25;
		private ushort _maxAttempts = 5;

		public DnsClientOptions()
		{

		}
	}
}