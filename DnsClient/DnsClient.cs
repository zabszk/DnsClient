using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DnsClient.Structs;

namespace DnsClient
{
	public class DnsClient
	{
		private readonly DnsClientOptions _options;
		private readonly UdpClient _client = new();
		internal static readonly Encoding Encoding = Encoding.ASCII;

		private readonly ConcurrentDictionary<ushort, DnsResponse> _responses = new();

		private ushort _transactionId;
		private readonly object _transactionIdLock = new();

		public DnsClient(IPEndPoint endPoint, DnsClientOptions? options = null)
		{
			_client.Connect(endPoint);
			_options = options ?? new();
		}

		public DnsClient(string address, int port = 53, DnsClientOptions? options = null)
		{
			_client.Connect(address, port);
			_options = options ?? new();
		}

		public async void Query(DnsQuery query)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(query.QueryLength);
			query.BuildQuery(buffer, AssignTransactionId());

			for (ushort i = 0; i < _options.MaxAtempts; i++) {
				await _client.SendAsync(buffer, query.QueryLength);
			}
		}

		#region Receiving

		private async void ReceiveLoop() //TODO Start the loop
		{
			while (true) //TODO Add end condition
			{
				var data = await _client.ReceiveAsync();

				//TODO Processing
			}
		}

		#endregion

		#region Transaction IDs
		private ushort AssignTransactionId()
		{
			lock (_transactionIdLock)
			{
				if (_transactionId < ushort.MaxValue)
					return _transactionId++;

				return _transactionId = 0;
			}
		}
		#endregion
	}
}