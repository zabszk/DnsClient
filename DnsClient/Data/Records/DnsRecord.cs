using System;
using System.Net;

namespace DnsClient.Data.Records;

public interface IDNSRecord
{
	uint TTL { get; set; }
}

public struct ARecord : IDNSRecord
{
	public IPAddress Address;
	public uint TTL { get; set; }

	internal static ARecord Parse(ArraySegment<byte> data)
	{
		return new ARecord
		{
			TTL = BitConverter.ToUInt32(data),
			Address = new IPAddress(data.Slice(6, BitConverter.ToInt32(data[4..])))
		};
	}
}

public struct AAAARecord : IDNSRecord
{
	public IPAddress Address;
	public uint TTL { get; set; }

	internal static AAAARecord Parse(ArraySegment<byte> data)
	{
		return new AAAARecord
		{
			TTL = BitConverter.ToUInt32(data),
			Address = new IPAddress(data.Slice(6, BitConverter.ToInt32(data[4..])))
		};
	}
}

