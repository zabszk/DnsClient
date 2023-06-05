using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DnsClient.Structs;

namespace DnsClient
{
	public class DnsClient
	{
		public DnsClientOptions Options;

		private readonly UdpClient _client = new();
		internal static readonly Encoding Encoding = Encoding.ASCII;

		private readonly ConcurrentDictionary<ushort, DnsQueryStatus> _transactions = new();

		private ushort _transactionId;
		private readonly object _transactionIdLock = new();

		public DnsClient(IPEndPoint endPoint, DnsClientOptions? options = null)
		{
			_client.Connect(endPoint);
			Options = options ?? new();
		}

		public DnsClient(string address, int port = 53, DnsClientOptions? options = null)
		{
			_client.Connect(address, port);
			Options = options ?? new();
		}

		public async Task<DnsResponse?> Query(DnsQuery query)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(query.QueryLength);
			ushort transactionId = AssignTransactionId();
			var status = new DnsQueryStatus(this);

			if (!_transactions.TryAdd(transactionId, status))
				throw new Exception("Couldn't create DNS transaction!");

			query.BuildQuery(buffer, transactionId);

			for (ushort i = 0; i < Options.MaxAtempts; i++) {
				await _client.SendAsync(buffer, query.QueryLength);
				if (await status.Wait())
					break;
			}

			if (status.State == DnsQueryStatus.Status.Completed)
				_transactions.TryRemove(transactionId, out _);

			return status.Response;
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
				return _transactionId < ushort.MaxValue
					? AbortTransactionIfExists(_transactionId++, false)
					: AbortTransactionIfExists(_transactionId = 0, false);
			}
		}

		private ushort AbortTransactionIfExists(ushort transactionId, bool success)
		{
			if (!_transactions.ContainsKey(transactionId) || !_transactions.TryRemove(transactionId, out var dnsQueryStatus))
				return transactionId;

			dnsQueryStatus.Abort(success);
			return transactionId;
		}
		#endregion
	}
}