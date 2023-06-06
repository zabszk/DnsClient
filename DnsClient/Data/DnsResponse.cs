using System.Collections.Generic;
using DnsClient.Data.Records;
using DnsClient.Enums;

namespace DnsClient.Data
{
	public class DnsResponse
	{
		public readonly DnsErrorCode ErrorCode;
		public List<DnsRecord.DNSRecord> Records;

		public DnsResponse(DnsErrorCode errorCode)
		{
			ErrorCode = errorCode;
		}
	}
}