using System;
using DnsClient.Enums;

namespace DnsClient.Structs
{
	public class DnsQuery
	{
		internal readonly int QueryLength = 17; //12 + 1 + 2 + 2 => header + null after the domain + query type + query class

		private readonly string[] _domain;
		private readonly QType _type;

		public DnsQuery(string name, QType type)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Name can't be null or whitespace.", nameof(name));

			_type = type;

			_domain = name.Split('.');

			foreach (var d in _domain)
				QueryLength += DnsClient.Encoding.GetByteCount(d) + 1;
		}

		internal void BuildQuery(byte[] buffer, ushort transactionId)
		{
			if (buffer.Length < QueryLength)
				throw new ArgumentException("Buffer is shorter than QueryLength!", nameof(buffer));

			//Transaction ID
			buffer[0] = (byte)(transactionId >> 8);
			buffer[1] = (byte)transactionId;

			//Flags
			buffer[2] = 0x01;
			buffer[3] = 0x00;

			//Amount of questions
			buffer[4] = 0;
			buffer[5] = 1;

			//Amount of answers RRs
			buffer[6] = 0;
			buffer[7] = 0;

			//Amount of authority RRs
			buffer[8] = 0;
			buffer[9] = 0;

			//Amount of additional RRs
			buffer[10] = 0;
			buffer[11] = 0;

			int b = 12;

			foreach (var d in _domain)
			{
				byte len = (byte)DnsClient.Encoding.GetByteCount(d);
				buffer[b++] = len;
				DnsClient.Encoding.GetBytes(d, 0, d.Length, buffer, b);
				b += len;
			}

			//String terminator
			buffer[b++] = 0;

			//Query type
			buffer[b++] = (byte)(((ushort)_type) >> 8);
			buffer[b++] = (byte)_type;

			//Query class (IN)
			buffer[b++] = 0;
			buffer[b] = 1;
		}
	}
}