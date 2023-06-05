using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Data;
using DnsClient.Enums;

namespace DnsClient
{
	public class DnsClient : IDisposable
	{
		public DnsClientOptions Options;

		private readonly Socket _socket;
		internal static readonly Encoding Encoding = Encoding.ASCII;

		private readonly ConcurrentDictionary<ushort, DnsQueryStatus> _transactions = new();
		private readonly CancellationTokenSource _ctSource = new();

		private ushort _transactionId;
		private readonly object _transactionIdLock = new();
		private readonly Thread _receiveThread;

		#region Constructors
		public DnsClient(EndPoint endPoint, DnsClientOptions? options = null)
		{
			_socket = new(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			_socket.Connect(endPoint);
			Options = options ?? new();

			_receiveThread = new Thread(ReceiveLoop)
			{
				IsBackground = true,
				Name = "DNS Client receive thread"
			};
			_receiveThread.Start();
		}

		public DnsClient(IPAddress address, ushort port = 53, DnsClientOptions? options = null)
		{
			_socket = new(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			_socket.Connect(address, port);
			Options = options ?? new();

			_receiveThread = new Thread(ReceiveLoop)
			{
				IsBackground = true,
				Name = "DNS Client receive thread"
			};
			_receiveThread.Start();
		}

		public DnsClient(string address, ushort port = 53, DnsClientOptions? options = null)
		{
			var addr = IPAddress.Parse(address);
			_socket = new(addr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			_socket.Connect(addr, port);
			Options = options ?? new();

			_receiveThread = new Thread(ReceiveLoop)
			{
				IsBackground = true,
				Name = "DNS Client receive thread"
			};
			_receiveThread.Start();
		}
		#endregion

		public async Task<DnsResponse?> Query(DnsQuery query)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(query.QueryLength);
			ushort transactionId = AssignTransactionId();
			var status = new DnsQueryStatus(this);

			if (!_transactions.TryAdd(transactionId, status))
				throw new Exception("Couldn't create DNS transaction!");

			query.BuildQuery(buffer, transactionId);

			for (ushort i = 0; i < Options.MaxAtempts; i++)
			{
				await _socket.SendAsync(new ArraySegment<byte>(buffer, 0, query.QueryLength), SocketFlags.None);
				if (await status.Wait())
					break;
			}

			ArrayPool<byte>.Shared.Return(buffer);
			_transactions.TryRemove(transactionId, out _);

			return status.Response;
		}

		#region Receiving

		private async void ReceiveLoop()
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(512);
			CancellationToken ct = _ctSource.Token;

			while (!ct.IsCancellationRequested)
			{
				try
				{
					var recv = await _socket.ReceiveAsync(buffer, SocketFlags.None, ct);

					if (ct.IsCancellationRequested)
						break;

					if (recv <= 0)
						continue;

					if (recv < 12)
					{
						Options.ErrorLogging?.LogError("Received a malformed UDP response (too short).");
						continue;
					}

					if ((buffer[2] & 0x80) == 0) //Received a query, not a response
						continue;

					ushort transactionId = BitConverter.ToUInt16(buffer, 0);

					if (!_transactions.TryGetValue(transactionId, out DnsQueryStatus query) || query.IsComplete)
						continue;

					byte errorCode = (byte)(buffer[3] & 0x0F);
					if (errorCode != 0) //Error returned
					{
						query.Abort((DnsErrorCode)errorCode);
						continue;
					}

					if (buffer[4] != 0 || buffer[5] != 1)
					{
						query.Abort(DnsErrorCode.CantParseResponse);
						continue;
					}

					ushort answers = BitConverter.ToUInt16(buffer, 6);
					int responseStart = 12;

					//Ignore queries
					for (; responseStart < recv; responseStart++)
						if (buffer[responseStart++] == 0)
							break;

					responseStart += 4; //Ignore query type and class

					//TODO Parse response
				}
				catch (TaskCanceledException)
				{
					break;
				}
				catch (Exception e)
				{
					Options.ErrorLogging?.LogException("Failed to receive data!", e);
				}
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

			dnsQueryStatus.Abort(DnsErrorCode.NoResponseFromServer);
			return transactionId;
		}
		#endregion

		public void Dispose()
		{
			GC.SuppressFinalize(this);

			_ctSource.Cancel();
		}

		~DnsClient() => Dispose();
	}
}