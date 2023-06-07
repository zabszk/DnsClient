using System;
using System.Net;
using DnsClient.Enums;

namespace DnsClient.Data.Records;

public static class DnsRecord
{
	internal static DNSRecord? Parse(QType type, ArraySegment<byte> data, uint ttl, byte[] rawResponse)
	{
		switch (type)
		{
			case QType.A:
				return ARecord.Parse(data, ttl);

			case QType.NS:
				return NSRecord.Parse(data, ttl, rawResponse);

			case QType.CNAME:
				return CNAMERecord.Parse(data, ttl, rawResponse);

			case QType.SOA:
				break;

			case QType.PTR:
				return PTRRecord.Parse(data, ttl, rawResponse);

			case QType.MX:
				return MXRecord.Parse(data, ttl, rawResponse);

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

	public class CNAMERecord : DNSRecord
	{
		public readonly string Alias;

		public QType Type => QType.CNAME;

		private CNAMERecord(uint ttl, string alias) : base(ttl) => Alias = alias;

		internal static CNAMERecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw) => new (ttl, Misc.Misc.ParseDomain(data, 0, out _, raw));

		public override string ToString() => $"CNAME: {Alias}";
	}

	public class NSRecord : DNSRecord
	{
		public readonly string NameServer;

		public QType Type => QType.NS;

		private NSRecord(uint ttl, string nameServer) : base(ttl) => NameServer = nameServer;

		internal static NSRecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw) => new (ttl, Misc.Misc.ParseDomain(data, 0, out _, raw));

		public override string ToString() => $"NS: {NameServer}";
	}

	public class PTRRecord : DNSRecord
	{
		public readonly string DomainName;

		public QType Type => QType.PTR;

		private PTRRecord(uint ttl, string domainName) : base(ttl) => DomainName = domainName;

		internal static PTRRecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw) => new (ttl, Misc.Misc.ParseDomain(data, 0, out _, raw));

		public override string ToString() => $"PTR: {DomainName}";
	}

	public class MXRecord : DNSRecord
	{
		public readonly string MailExchange;

		public readonly ushort Preference;

		public QType Type => QType.MX;

		private MXRecord(uint ttl, string mailExchange, ushort preference) : base(ttl)
		{
			MailExchange = mailExchange;
			Preference = preference;
		}

		internal static MXRecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw) => new (ttl, Misc.Misc.ParseDomain(data, 2, out _, raw), (ushort)((data[0] << 8) | data[1]));

		public override string ToString() => $"MX: {MailExchange} (Preference: {Preference})";
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
