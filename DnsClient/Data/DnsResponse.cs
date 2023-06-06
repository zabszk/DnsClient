using System.Collections.Generic;
using DnsClient.Data.Records;
using DnsClient.Enums;

namespace DnsClient.Data
{
	public class DnsResponse
	{
		public readonly DnsErrorCode ErrorCode;
		public readonly List<DnsRecord.DNSRecord>? Records;

		internal DnsResponse(DnsErrorCode errorCode, List<DnsRecord.DNSRecord>? records = null)
		{
			ErrorCode = errorCode;
			Records = records;
		}
	}
}