using System;
using System.Net;
using DnsClient.Enums;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

namespace DnsClient.Data.Records;

/// <summary>
/// A class responsible for parsing and representing DNS records
/// </summary>
public static class DnsRecord
{
	internal static DNSRecord? Parse(QType type, ArraySegment<byte> data, uint ttl, byte[] rawResponse)
	{
		return type switch
		{
			QType.A => ARecord.Parse(data, ttl),
			QType.NS => NSRecord.Parse(data, ttl, rawResponse),
			QType.CNAME => CNAMERecord.Parse(data, ttl, rawResponse),
			QType.SOA => SOARecord.Parse(data, ttl, rawResponse),
			QType.PTR => PTRRecord.Parse(data, ttl, rawResponse),
			QType.MX => MXRecord.Parse(data, ttl, rawResponse),
			QType.TXT => TXTRecord.Parse(data, ttl),
			QType.AAAA => AAAARecord.Parse(data, ttl),
			QType.SRV => SRVRecord.Parse(data, ttl, rawResponse),
			QType.DS => DSRecord.Parse(data, ttl),
			QType.DNSKEY => DNSKEYRecord.Parse(data, ttl),
			QType.CAA => CAARecord.Parse(data, ttl),
			QType.URI => URIRecord.Parse(data, ttl),
			_ => new UnknownRecord(ttl)
		};
	}

	/// <summary>
	/// A class representing any type of DNS record
	/// </summary>
	public abstract class DNSRecord
	{
		/// <summary>
		/// DNS record TTL value
		/// </summary>
		// ReSharper disable once InconsistentNaming
		public readonly uint TTL;

		/// <summary>
		/// DNS record type
		/// </summary>
		public abstract QType Type { get; }

		internal DNSRecord(uint ttl) => TTL = ttl;
	}

	/// <summary>
	/// Unknown type of DNS record
	/// </summary>
	public class UnknownRecord : DNSRecord
	{
		/// <inheritdoc />
		public override QType Type => QType.Unknown;

		internal UnknownRecord(uint ttl) : base(ttl) { }

		/// <inheritdoc />
		public override string ToString() => "(unsupported DNS record)";
	}

	/// <summary>
	/// Class representing either A or AAAA DNS record
	/// </summary>
	public abstract class IPAddressRecord : DNSRecord
	{
		/// <summary>
		/// IP address pointed by the record
		/// </summary>
		// ReSharper disable once MemberCanBeProtected.Global
		public readonly IPAddress Address;

		/// <summary>
		/// Class constructor
		/// </summary>
		/// <param name="ttl">TTL value of the record</param>
		/// <param name="address">IP address pointed by the record</param>
		protected IPAddressRecord(uint ttl, IPAddress address) : base(ttl) => Address = address;
	}

	/// <summary>
	/// A DNS record
	/// </summary>
	public class ARecord : IPAddressRecord
	{
		/// <inheritdoc />
		public override QType Type => QType.A;

		private ARecord(uint ttl, IPAddress address) : base(ttl, address) { }

		internal static ARecord? Parse(ArraySegment<byte> data, uint ttl) => data.Count != 4 ? null : new ARecord(ttl, new IPAddress(data));

		/// <inheritdoc />
		public override string ToString() => $"A: {Address}";
	}

	/// <summary>
	/// AAAA DNS record
	/// </summary>
	public class AAAARecord : IPAddressRecord
	{
		/// <inheritdoc />
		public override QType Type => QType.AAAA;

		private AAAARecord(uint ttl, IPAddress address) : base(ttl, address) { }

		internal static AAAARecord? Parse(ArraySegment<byte> data, uint ttl) => data.Count != 16 ? null : new AAAARecord(ttl, new IPAddress(data));

		/// <inheritdoc />
		public override string ToString() => $"AAAA: {Address}";
	}

	/// <summary>
	/// CNAME DNS record
	/// </summary>
	public class CNAMERecord : DNSRecord
	{
		/// <summary>
		/// Domain pointed by the record
		/// </summary>
		public readonly string Alias;

		/// <inheritdoc />
		public override QType Type => QType.CNAME;

		private CNAMERecord(uint ttl, string alias) : base(ttl) => Alias = alias;

		internal static CNAMERecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw) => new (ttl, Misc.Misc.ParseDomain(data, 0, out _, raw));

		/// <inheritdoc />
		public override string ToString() => $"CNAME: {Alias}";
	}

	/// <summary>
	/// SOA DNS record
	/// </summary>
	public class SOARecord : DNSRecord
	{
		/// <summary>
		/// Primary Name Server
		/// </summary>
		public readonly string PrimaryNameServer;

		/// <summary>
		/// Responsible Authority Mailbox
		/// </summary>
		public readonly string ResponsibleAuthorityMailbox;

		/// <summary>
		/// Serial Number
		/// </summary>
		public readonly uint SerialNumber;

		/// <summary>
		/// Refresh Interval
		/// </summary>
		public readonly uint RefreshInterval;

		/// <summary>
		/// Retry Interval
		/// </summary>
		public readonly uint RetryInterval;

		/// <summary>
		/// Expire Limit
		/// </summary>
		public readonly uint ExpireLimit;

		/// <summary>
		/// Minimum TTL
		/// </summary>
		public readonly uint MinimumTTL;

		/// <inheritdoc />
		public override QType Type => QType.SOA;

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

		/// <inheritdoc />
		public override string ToString() => $"SOA: {Environment.NewLine}Primary New Server: {PrimaryNameServer}{Environment.NewLine}Responsible Authority Mailbox: {ResponsibleAuthorityMailbox}{Environment.NewLine}Serial Number: {SerialNumber}{Environment.NewLine}Refresh Interval: {RefreshInterval}{Environment.NewLine}Retry Interval: {RetryInterval}{Environment.NewLine}Expire Limit: {ExpireLimit}{Environment.NewLine}Minimum TTL: {MinimumTTL}";
	}

	/// <summary>
	/// NS DNS record
	/// </summary>
	public class NSRecord : DNSRecord
	{
		/// <summary>
		/// Name Server address pointed by the record
		/// </summary>
		public readonly string NameServer;

		/// <inheritdoc />
		public override QType Type => QType.NS;

		private NSRecord(uint ttl, string nameServer) : base(ttl) => NameServer = nameServer;

		internal static NSRecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw) => new (ttl, Misc.Misc.ParseDomain(data, 0, out _, raw));

		/// <inheritdoc />
		public override string ToString() => $"NS: {NameServer}";
	}

	/// <summary>
	/// PTR DNS record
	/// </summary>
	public class PTRRecord : DNSRecord
	{
		/// <summary>
		/// Domain Name pointed by the record
		/// </summary>
		public readonly string DomainName;

		/// <inheritdoc />
		public override QType Type => QType.PTR;

		private PTRRecord(uint ttl, string domainName) : base(ttl) => DomainName = domainName;

		internal static PTRRecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw) => new (ttl, Misc.Misc.ParseDomain(data, 0, out _, raw));

		/// <inheritdoc />
		public override string ToString() => $"PTR: {DomainName}";
	}

	/// <summary>
	/// MX DNS record
	/// </summary>
	public class MXRecord : DNSRecord
	{
		/// <summary>
		/// Mail Exchange server address pointed by the record
		/// </summary>
		public readonly string MailExchange;

		/// <summary>
		/// Mail Exchange server preference value
		/// </summary>
		public readonly ushort Preference;

		/// <inheritdoc />
		public override QType Type => QType.MX;

		private MXRecord(uint ttl, string mailExchange, ushort preference) : base(ttl)
		{
			MailExchange = mailExchange;
			Preference = preference;
		}

		internal static MXRecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw) => new (ttl, Misc.Misc.ParseDomain(data, 2, out _, raw), (ushort)((data[0] << 8) | data[1]));

		/// <inheritdoc />
		public override string ToString() => $"MX: {MailExchange} (Preference: {Preference})";
	}

	/// <summary>
	/// TXT DNS record
	/// </summary>
	public class TXTRecord : DNSRecord
	{
		/// <summary>
		/// Text of the DNS record
		/// </summary>
		public readonly string Text;

		/// <inheritdoc />
		public override QType Type => QType.TXT;

		private TXTRecord(uint ttl, string text) : base(ttl) => Text = text;

		internal static TXTRecord? Parse(ArraySegment<byte> data, uint ttl) => data[0] != data.Count - 1 ? null : new TXTRecord(ttl, DnsClient.Encoding.GetString(data[1..]));

		/// <inheritdoc />
		public override string ToString() => $"TXT: {Text}";
	}

	/// <summary>
	/// SRV DNS record
	/// </summary>
	public class SRVRecord : DNSRecord
	{
		/// <summary>
		/// DNS record priority
		/// </summary>
		public readonly ushort Priority;

		/// <summary>
		/// DNS record weight
		/// </summary>
		public readonly ushort Weight;

		/// <summary>
		/// Port of the service
		/// </summary>
		public readonly ushort Port;

		/// <summary>
		/// Address of the service
		/// </summary>
		public readonly string Target;

		/// <inheritdoc />
		public override QType Type => QType.SRV;

		private SRVRecord(uint ttl, ushort priority, ushort weight, ushort port, string target) : base(ttl)
		{
			Priority = priority;
			Weight = weight;
			Port = port;
			Target = target;
		}

		internal static SRVRecord Parse(ArraySegment<byte> data, uint ttl, byte[] raw) => new SRVRecord(ttl, (ushort)((data[0] << 8) | data[1]), (ushort)((data[2] << 8) | data[3]), (ushort)((data[4] << 8) | data[5]), Misc.Misc.ParseDomain(data, 6, out _, raw));

		/// <inheritdoc />
		public override string ToString() => $"SRV: {Target} (Priority: {Priority}, Weight: {Weight}, Port: {Port})";
	}

	/// <summary>
	/// CAA DNS record
	/// </summary>
	public class CAARecord : DNSRecord
	{
		/// <summary>
		/// DNS record flags
		/// </summary>
		public readonly byte Flags;

		/// <summary>
		/// DNS record tag
		/// </summary>
		public readonly string Tag;

		/// <summary>
		/// DNS record value
		/// </summary>
		public readonly string Value;

		/// <inheritdoc />
		public override QType Type => QType.CAA;

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

		/// <inheritdoc />
		public override string ToString() => $"CAA: {Flags} {Tag} {Value}";
	}

	/// <summary>
	/// DS DNS record
	/// </summary>
	public class DSRecord : DNSRecord
	{
		/// <summary>
		/// KeyId
		/// </summary>
		public readonly ushort KeyId;

		/// <summary>
		/// Algorithm
		/// </summary>
		public readonly byte Algorithm;

		/// <summary>
		/// Type of the digest
		/// </summary>
		public readonly byte DigestType;

		/// <summary>
		/// Digest
		/// </summary>
		public readonly byte[] Digest;

		/// <inheritdoc />
		public override QType Type => QType.DS;

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

		/// <inheritdoc />
		public override string ToString() => $"DS:{Environment.NewLine}Key ID: {KeyId}{Environment.NewLine}Algorithm: {Algorithm}{Environment.NewLine}Digest Type: {DigestType}{Environment.NewLine}Digest: {BitConverter.ToString(Digest).Replace("-", string.Empty, StringComparison.Ordinal)}";
	}

	/// <summary>
	/// DNSKEY DNS record
	/// </summary>
	public class DNSKEYRecord : DNSRecord
	{
		/// <summary>
		/// DNS record flags
		/// </summary>
		public readonly ushort Flags;

		/// <summary>
		/// Protocol
		/// </summary>
		public readonly byte Protocol;

		/// <summary>
		/// Algorithm
		/// </summary>
		public readonly byte Algorithm;

		/// <summary>
		/// Public Key
		/// </summary>
		public readonly byte[] PublicKey;

		/// <inheritdoc />
		public override QType Type => QType.DNSKEY;

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

		/// <inheritdoc />
		public override string ToString() => $"DNSKEY:{Environment.NewLine}Flags: {Flags}{Environment.NewLine}Protocol: {Protocol}{Environment.NewLine}Algorithm: {Algorithm}{Environment.NewLine}PublicKey: {BitConverter.ToString(PublicKey).Replace("-", string.Empty, StringComparison.Ordinal)}";
	}

	/// <summary>
	/// URI DNS record
	/// </summary>
	public class URIRecord : DNSRecord
	{
		/// <summary>
		/// Priority of the DNS record
		/// </summary>
		public readonly ushort Priority;

		/// <summary>
		/// Weight of the DNS record
		/// </summary>
		public readonly ushort Weight;

		/// <summary>
		/// Text of the DNS record
		/// </summary>
		public readonly string Target;

		/// <inheritdoc />
		public override QType Type => QType.URI;

		private URIRecord(uint ttl, ushort priority, ushort weight, string target) : base(ttl)
		{
			Priority = priority;
			Weight = weight;
			Target = target;
		}

		internal static URIRecord Parse(ArraySegment<byte> data, uint ttl) => new URIRecord(ttl, (ushort)((data[0] << 8) | data[1]), (ushort)((data[2] << 8) | data[3]), DnsClient.Encoding.GetString(data[4..]));

		/// <inheritdoc />
		public override string ToString() => $"URI: {Target} (Priority: {Priority}, Weight: {Weight})";
	}
}
