using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace DnsClient.Misc;

public static class Misc
{
	#region Domain parsing
	internal static string ParseDomain(ArraySegment<byte> data, int startIndex, out int read, byte[] rawResponse)
	{
		string domain = string.Empty;
		read = startIndex;

		while (data[read] != 0)
		{
			byte len = data[read++];

			if (len == 0xc0) //Pointer
			{
				domain += "." + ParseDomain(rawResponse, data[read], out _, rawResponse);
				break;
			}

			int remaining = data.Count - read;

			if (remaining < len)
				break;

			if (domain != "")
				domain += ".";

			domain += DnsClient.Encoding.GetString(data.Slice(read, len).ToArray());
			read += len;
		}

		read++;
		read -= startIndex;
		return domain;
	}

	internal static string ParseDomain(byte[] data, int startIndex, out int read, byte[] rawResponse)
	{
		string domain = string.Empty;
		read = startIndex;

		while (data[read] != 0)
		{
			byte len = data[read++];

			if (len == 0xc0) //Pointer
			{
				domain += "." + ParseDomain(rawResponse, data[read], out _, rawResponse);
				break;
			}

			int remaining = data.Length - read;

			if (remaining < len)
				break;

			if (domain != "")
				domain += ".";

			domain += DnsClient.Encoding.GetString(data, read, len);
			read += len;
		}

		read++;
		read -= startIndex;
		return domain;
	}
	#endregion

	#region PTR
	public static string GetPtrAddress(string address) => GetPtrAddress(IPAddress.Parse(address));

	public static bool TryGetPtrAddress(string address, out string? ptrAddress)
	{
		if (IPAddress.TryParse(address, out var ptr))
		{
			ptrAddress = GetPtrAddress(ptr);
			return true;
		}

		ptrAddress = null;
		return false;
	}

	public static string GetPtrAddress(IPAddress address)
	{
		switch (address.AddressFamily)
		{
			case AddressFamily.InterNetwork:
				return string.Join('.', address.ToString().Split(".").Reverse()) + ".in-addr.arpa";

			case AddressFamily.InterNetworkV6:
				return "";
				break;

			default:
				throw new NotSupportedException();
		}
	}
	#endregion
}