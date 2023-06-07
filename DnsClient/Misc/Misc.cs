using System;

namespace DnsClient.Misc;

internal static class Misc
{
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
}