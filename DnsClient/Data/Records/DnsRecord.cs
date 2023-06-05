using System;
using System.Net;

namespace DnsClient.Data.Records;

public struct ARecord
{
	public IPAddress Address;
	public uint TTL;

	internal ARecord Parse(ArraySegment<byte> data)
	{
		return new ARecord()
		{
			TTL = BitConverter.ToUInt32(data),
			Address = new IPAddress(data.Slice(6, BitConverter.ToInt32(data[4..])))
		};
	}
}

public struct AAAARecord
{
	public IPAddress Address;
	public uint TTL;

	internal AAAARecord Parse(ArraySegment<byte> data)
	{
		return new AAAARecord()
		{
			TTL = BitConverter.ToUInt32(data),
			Address = new IPAddress(data.Slice(6, BitConverter.ToInt32(data[4..])))
		};
	}
}

