using System.Diagnostics.CodeAnalysis;

namespace DnsClient.Enums;

/// <summary>
/// QType of the DNS query or record
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum QType : ushort
{
#pragma warning disable CS1591
	Unknown = 0,
	A = 1,
	NS = 2,
	CNAME = 5,
	SOA = 6,
	PTR = 12,
	MX = 15,
	TXT = 16,
	AAAA = 28,
	SRV = 33,
	DS = 43,
	DNSKEY = 48,
	CAA = 257
#pragma warning restore CS1591
}