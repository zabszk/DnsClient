using DnsClient.Enums;

namespace DnsClient.Data
{
	public class DnsResponse
	{
		public readonly DnsErrorCode ErrorCode;

		public DnsResponse(DnsErrorCode errorCode)
		{
			ErrorCode = errorCode;
		}
	}
}