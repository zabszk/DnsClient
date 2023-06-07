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
		/// Records returned by the DNS server
		/// </summary>
		public readonly List<DnsRecord.DNSRecord>? Records;

		internal DnsResponse(DnsErrorCode errorCode, List<DnsRecord.DNSRecord>? records = null)
		{
			ErrorCode = errorCode;
			Records = records;
		}
	}
}