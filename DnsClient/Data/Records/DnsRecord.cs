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
				return SOARecord.Parse(data, ttl, rawResponse);

			case QType.PTR:
				return PTRRecord.Parse(data, ttl, rawResponse);

			case QType.MX:
				return MXRecord.Parse(data, ttl, rawResponse);

			case QType.TXT:
				return TXTRecord.Parse(data, ttl);

			case QType.AAAA:
				return AAAARecord.Parse(data, ttl);

			case QType.SRV:
				return SRVRecord.Parse(data, ttl, rawResponse);

			case QType.DS:
				return DSRecord.Parse(data, ttl);

			case QType.DNSKEY:
				return DNSKEYRecord.Parse(data, ttl);

			case QType.CAA:
				return CAARecord.Parse(data, ttl);

			default:
				return new UnknownRecord(ttl);
		}
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

	public class SOARecord : DNSRecord
	{
		public readonly string PrimaryNameServer;

		public readonly string ResponsibleAuthorityMailbox;

		public readonly uint SerialNumber;

		public readonly uint RefreshInterval;

		public readonly uint RetryInterval;

		public readonly uint ExpireLimit;

		public readonly uint MinimumTTL;

		public QType Type => QType.SOA;

		private SOARecord(uint ttl, string primaryNameServer, string responsibleAuthorityMailbox, uint serialNumber, uint refreshInterval, uint retryInterval, uint expireLimit, uint minimumTTL) : base(ttl)
		{
			PrimaryNameServer = primaryNameServer;
			ResponsibleAuthorityMailbox = responsibleAuthorityMailbox;
			SerialNumber = serialNumber;
			RefreshInterval = refreshInterval;
			RetryInterval = retryInterval;
			ExpireLimit = expireLimit;
			MinimumTTL = minimumTTL;
		}

		internal static SOARecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw)
		{
			string pns = Misc.Misc.ParseDomain(data, 0, out var index, raw);
			string ram = Misc.Misc.ParseDomain(data, index, out var read, raw);
			read += index;

			uint sn = (uint)((data[read] << 24) | (data[read + 1] << 16) | (data[read + 2] << 8) | (data[read + 3]));
			read += 4;

			uint refI = (uint)((data[read] << 24) | (data[read + 1] << 16) | (data[read + 2] << 8) | (data[read + 3]));
			read += 4;

			uint retI = (uint)((data[read] << 24) | (data[read + 1] << 16) | (data[read + 2] << 8) | (data[read + 3]));
			read += 4;

			uint expLim = (uint)((data[read] << 24) | (data[read + 1] << 16) | (data[read + 2] << 8) | (data[read + 3]));
			read += 4;

			uint minTTL = (uint)((data[read] << 24) | (data[read + 1] << 16) | (data[read + 2] << 8) | (data[read + 3]));

			return new SOARecord(ttl, pns, ram, sn, refI, retI, expLim, minTTL);
		}

		public override string ToString() => $"SOA: {Environment.NewLine}Primary New Server: {PrimaryNameServer}{Environment.NewLine}Responsible Authority Mailbox: {ResponsibleAuthorityMailbox}{Environment.NewLine}Serial Number: {SerialNumber}{Environment.NewLine}Refresh Interval: {RefreshInterval}{Environment.NewLine}Retry Interval: {RetryInterval}{Environment.NewLine}Expire Limit: {ExpireLimit}{Environment.NewLine}Minimum TTL: {MinimumTTL}";
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

	public class SRVRecord : DNSRecord
	{
		public readonly ushort Priority;

		public readonly ushort Weight;

		public readonly ushort Port;

		public readonly string Target;

		public QType Type => QType.SRV;

		private SRVRecord(uint ttl, ushort priority, ushort weight, ushort port, string target) : base(ttl)
		{
			Priority = priority;
			Weight = weight;
			Port = port;
			Target = target;
		}

		internal static SRVRecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw) => new SRVRecord(ttl, (ushort)((data[0] << 8) | data[1]), (ushort)((data[2] << 8) | data[3]), (ushort)((data[4] << 8) | data[5]), Misc.Misc.ParseDomain(data, 6, out _, raw));

		public override string ToString() => $"SRV: {Target} (Priority: {Priority}, Weight: {Weight}, Port: {Port})";
	}

	public class CAARecord : DNSRecord
	{
		public readonly byte Flags;

		public readonly string Tag;

		public readonly string Value;

		public QType Type => QType.CAA;

		private CAARecord(uint ttl, byte flags, string tag, string value) : base(ttl)
		{
			Flags = flags;
			Tag = tag;
			Value = value;
		}

		internal static CAARecord Parse(ArraySegment<byte> data, uint ttl)
		{
			byte tagLength = data[1];
			return new CAARecord(ttl, data[0], DnsClient.Encoding.GetString(data[2..(tagLength + 2)]), DnsClient.Encoding.GetString(data[(tagLength + 2)..]));
		}

		public override string ToString() => $"CAA: {Flags} {Tag} {Value}";
	}

	public class DSRecord : DNSRecord
	{
		public readonly ushort KeyId;

		public readonly byte Algorithm;

		public readonly byte DigestType;

		public readonly byte[] Digest;

		public QType Type => QType.DS;

		private DSRecord(uint ttl, ushort keyId, byte algorithm, byte digestType, byte[] digest) : base(ttl)
		{
			KeyId = keyId;
			Algorithm = algorithm;
			DigestType = digestType;
			Digest = digest;
		}

		internal static DSRecord Parse(ArraySegment<byte> data, uint ttl)
		{
			byte[] digest = new byte[data.Count - 4];
			for (int i = 0; i < digest.Length; i++)
				digest[i] = data[i + 4];

			return new DSRecord(ttl, (ushort)((data[0] << 8) | data[1]), data[2], data[3], digest);
		}

		public override string ToString() => $"DS:{Environment.NewLine}Key ID: {KeyId}{Environment.NewLine}Algorithm: {Algorithm}{Environment.NewLine}Digest Type: {DigestType}{Environment.NewLine}Digest: {BitConverter.ToString(Digest).Replace("-", string.Empty, StringComparison.Ordinal)}";
	}

	public class DNSKEYRecord : DNSRecord
	{
		public readonly ushort Flags;

		public readonly byte Protocol;

		public readonly byte Algorithm;

		public readonly byte[] PublicKey;

		public QType Type => QType.DNSKEY;

		private DNSKEYRecord(uint ttl, ushort flags, byte protocol, byte algorithm, byte[] publicKey) : base(ttl)
		{
			Flags = flags;
			Protocol = protocol;
			Algorithm = algorithm;
			PublicKey = publicKey;
		}

		internal static DNSKEYRecord Parse(ArraySegment<byte> data, uint ttl)
		{
			byte[] publicKey = new byte[data.Count - 4];
			for (int i = 0; i < publicKey.Length; i++)
				publicKey[i] = data[i + 4];

			return new DNSKEYRecord(ttl, (ushort)((data[0] << 8) | data[1]), data[2], data[3], publicKey);
		}

		public override string ToString() => $"DNSKEY:{Environment.NewLine}Flags: {Flags}{Environment.NewLine}Protocol: {Protocol}{Environment.NewLine}Algorithm: {Algorithm}{Environment.NewLine}PublicKey: {BitConverter.ToString(PublicKey).Replace("-", string.Empty, StringComparison.Ordinal)}";
	}
}
