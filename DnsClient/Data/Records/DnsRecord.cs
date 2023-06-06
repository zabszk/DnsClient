using System;
using System.Net;
using DnsClient.Enums;

namespace DnsClient.Data.Records;

public static class DnsRecord
{
	internal static DNSRecord? Parse(QType type, ArraySegment<byte> data, uint ttl)
	{
		switch (type)
		{
			case QType.A:
				return ARecord.Parse(data, ttl);

			case QType.NS:
				break;

			case QType.CNAME:
				break;

			case QType.SOA:
				break;

			case QType.PTR:
				break;

			case QType.MX:
				break;

			case QType.TXT:
				return TXTRecord.Parse(data, ttl);

			case QType.AAAA:
				return AAAARecord.Parse(data, ttl);

			case QType.SRV:
				break;

			default:
				return new UnknownRecord(ttl);
		}

		return null;
	}

	public abstract class DNSRecord
	{
		public readonly uint TTL;

		public QType Type { get; }

		internal DNSRecord(uint ttl) => TTL = ttl;
	}

	public class UnknownRecord : DNSRecord
	{
		public QType Type => QType.Unknown;

		internal UnknownRecord(uint ttl) : base(ttl) { }

		public override string ToString() => "(unsupported DNS record)";
	}

	public class ARecord : DNSRecord
	{
		public readonly IPAddress Address;
		public QType Type => QType.A;

		private ARecord(uint ttl, IPAddress address) : base(ttl) => Address = address;

		internal static ARecord? Parse(ArraySegment<byte> data, uint ttl) => data.Count != 4 ? null : new ARecord(ttl, new IPAddress(data));

		public override string ToString() => $"A: {Address}";
	}

	public class AAAARecord : DNSRecord
	{
		public readonly IPAddress Address;
		public QType Type => QType.AAAA;

		private AAAARecord(uint ttl, IPAddress address) : base(ttl) => Address = address;

		internal static AAAARecord? Parse(ArraySegment<byte> data, uint ttl) => data.Count != 16 ? null : new AAAARecord(ttl, new IPAddress(data));

		public override string ToString() => $"AAAA: {Address}";
	}

	public class TXTRecord : DNSRecord
	{
		public readonly string Text;
		public QType Type => QType.TXT;

		private TXTRecord(uint ttl, string text) : base(ttl) => Text = text;

		internal static TXTRecord? Parse(ArraySegment<byte> data, uint ttl) => data[0] != data.Count - 1 ? null : new TXTRecord(ttl, DnsClient.Encoding.GetString(data[1..]));

		public override string ToString() => $"TXT: {Text}";
	}
}
