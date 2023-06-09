using System;
using DnsClient.Enums;

namespace DnsClient.Data
{
	/// <summary>
	/// DNS Query
	/// </summary>
	public class DnsQuery
	{
		internal readonly int QueryLength = 17; //12 + 1 + 2 + 2 => header + null after the domain + query type + query class

		private readonly string[] _domain;
		private readonly QType[] _type;
		internal readonly bool AcceptTruncated;

		/// <summary>
		/// Constructor of a DNS query
		/// </summary>
		/// <param name="name">Domain to query</param>
		/// <param name="type">DNS record QType to obtain</param>
		/// <param name="acceptTruncated">If set to true, the DNS client will accept truncated responses and won't retry the query using TCP</param>
		public DnsQuery(string name, QType type, bool acceptTruncated) : this(name, new[] {type}, acceptTruncated) { }

		/// <summary>
		/// Constructor of a DNS query.
		/// WARNING: Most of the DNS servers DOES NOT support querying multiple QTypes at once and will only respond to the first type.
		/// </summary>
		/// <param name="name">Domain to query</param>
		/// <param name="type">Array of DNS record QTypes to obtain</param>
		/// <param name="acceptTruncated">If set to true, the DNS client will accept truncated responses and won't retry the query using TCP</param>
		public DnsQuery(string name, QType[] type, bool acceptTruncated = false)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Name can't be null or whitespace.", nameof(name));

			if (type == null || type.Length == 0)
				throw new ArgumentException("Type can't be null or empty.", nameof(type));

			if (type.Length > 255)
				throw new ArgumentException("Too big array of types", nameof(type));

			_type = type;
			AcceptTruncated = acceptTruncated;

			_domain = name.Split('.');

			foreach (var d in _domain)
				QueryLength += DnsClient.Encoding.GetByteCount(d) + 1;

			QueryLength += (_type.Length - 1) * 6; //Pointer + query type + query class
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
			buffer[5] = (byte)_type.Length;

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
			bool insertPointer = false;

			foreach (var d in _domain)
			{
				byte len = (byte)DnsClient.Encoding.GetByteCount(d);
				buffer[b++] = len;
				DnsClient.Encoding.GetBytes(d, 0, d.Length, buffer, b);
				b += len;
			}

			//String terminator
			buffer[b++] = 0;

			foreach (var t in _type)
			{
				if (insertPointer)
				{
					buffer[b++] = 0xc0;
					buffer[b++] = 12;
				}
				else insertPointer = true;

				//Query type
				buffer[b++] = (byte)(((ushort)t) >> 8);
				buffer[b++] = (byte)t;

				//Query class (IN)
				buffer[b++] = 0;
				buffer[b++] = 1;
			}
		}
	}
}