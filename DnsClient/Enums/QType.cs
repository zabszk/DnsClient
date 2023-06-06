namespace DnsClient.Enums
{
	public enum QType : ushort
	{
		Unknown = 0,
		A = 1,
		NS = 2,
		CNAME = 5,
		SOA = 6,
		PTR = 12,
		MX = 15,
		TXT = 16,
		AAAA = 28,
		SRV = 33
	}
}