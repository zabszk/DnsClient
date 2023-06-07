using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DnsClient.Misc;

/// <summary>
/// Class containing helper methods
/// </summary>
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

	#region PT
	/// <summary>
	/// Attempts to return domain to query in order to query PTR record of an IP address
	/// </summary>
	/// <param name="address">IP address to query</param>
	/// <param name="ptrAddress">A domain to query</param>
	/// <returns>Indicates whether the operation was successful</returns>
	public static bool TryGetPtrAddress(string address, out string? ptrAddress)
	{
		if (IPAddress.TryParse(address, out var ptr))
			return TryGetPtrAddress(ptr, out ptrAddress);

		ptrAddress = null;
		return false;
	}

	/// <summary>
	/// Returns domain to query in order to query PTR record of an IP address
	/// </summary>
	/// <param name="address">IP address to query</param>
	/// <returns>A domain to query</returns>
	/// <exception cref="Exception">Can't parse provided IP address</exception>
	/// <exception cref="NotSupportedException">AddressFamily not supported</exception>
	public static string GetPtrAddress(string address) => GetPtrAddress(IPAddress.Parse(address));

	/// <summary>
	/// Returns domain to query in order to query PTR record of an IP address
	/// </summary>
	/// <param name="address">IP address to query</param>
	/// <returns>A domain to query</returns>
	/// <exception cref="Exception">Can't parse provided IP address</exception>
	/// <exception cref="NotSupportedException">AddressFamily not supported</exception>
	public static string GetPtrAddress(IPAddress address)
	{
		switch (address.AddressFamily)
		{
			case AddressFamily.InterNetwork:
				return string.Join('.', address.ToString().Split(".").Reverse()) + ".in-addr.arpa";

			case AddressFamily.InterNetworkV6:
				byte[] addr = ArrayPool<byte>.Shared.Rent(16);

				try
				{
					if (!address.TryWriteBytes(addr, out int written) || written != 16)
						throw new Exception("Can't parse IPv6 address");

					string str = BitConverter.ToString(addr).Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
					StringBuilder sb = new(72);

					for (int i = str.Length - 1; i >= 0; i--)
					{
						sb.Append(str[i]);
						sb.Append('.');
					}

					sb.Append("ip6.arpa");
					return sb.ToString();
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(addr);
				}

			default:
				throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Attempts to return domain to query in order to query PTR record of an IP address
	/// </summary>
	/// <param name="address">IP address to query</param>
	/// <param name="ptrAddress">A domain to query</param>
	/// <returns>Indicates whether the operation was successful</returns>
	// ReSharper disable once MemberCanBePrivate.Global
	public static bool TryGetPtrAddress(IPAddress address, out string? ptrAddress)
	{
		switch (address.AddressFamily)
		{
			case AddressFamily.InterNetwork:
				ptrAddress = string.Join('.', address.ToString().Split(".").Reverse()) + ".in-addr.arpa";
				return true;

			case AddressFamily.InterNetworkV6:
				byte[] addr = ArrayPool<byte>.Shared.Rent(16);

				try
				{
					if (!address.TryWriteBytes(addr, out int written) || written != 16)
					{
						ptrAddress = null;
						return false;
					}

					string str = BitConverter.ToString(addr).Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
					StringBuilder sb = new(72);

					for (int i = str.Length - 1; i >= 0; i--)
					{
						sb.Append(str[i]);
						sb.Append('.');
					}

					sb.Append("ip6.arpa");
					ptrAddress = sb.ToString();
					return true;
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(addr);
				}

			default:
				ptrAddress = null;
				return false;
		}
	}
	#endregion
}