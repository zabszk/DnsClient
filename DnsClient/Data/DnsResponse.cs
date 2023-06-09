using System.Collections.Generic;
using DnsClient.Data.Records;
using DnsClient.Enums;

namespace DnsClient.Data
{
	/// <summary>
	/// Response to a DNS Query
	/// </summary>
	public class DnsResponse
	{
		/// <summary>
		/// Error code returned by the DNS server
		/// </summary>
		public readonly DnsErrorCode ErrorCode;

		/// <summary>
		/// Indicates whether the response was too long and was truncated by the DNS server
		/// If this equals to true, it means that not all DNS records are present in this response.
		/// </summary>
		public readonly bool Truncated;

		/// <summary>
		/// Records returned by the DNS server
		/// </summary>
		public readonly List<DnsRecord.DNSRecord>? Records;

		internal DnsResponse(DnsErrorCode errorCode, bool truncated = false, List<DnsRecord.DNSRecord>? records = null)
		{
			ErrorCode = errorCode;
			Truncated = truncated;
			Records = records;
		}
	}
}