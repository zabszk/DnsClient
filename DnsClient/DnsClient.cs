using System;
using System.Buffers;
using System.Buffers.Binary;
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
	/// <summary>
	/// Client of the DNS protocol
	/// </summary>
	public class DnsClient : IDisposable
	{
		/// <summary>
		/// Options of the DNS client
		/// </summary>
		public DnsClientOptions Options;

		private readonly Socket _socket;
		internal static readonly Encoding Encoding = Encoding.ASCII;

		private readonly ConcurrentDictionary<ushort, DnsQueryStatus> _transactions = new();
		private readonly CancellationTokenSource _ctSource = new();

		private ushort _transactionId;
		private readonly object _transactionIdLock = new();

		#region Constructors
		/// <summary>
		/// Constructor of the DNS Client
		/// </summary>
		/// <param name="endPoint">Endpoint of the DNS server</param>
		/// <param name="options">Options of the DNS client</param>
		public DnsClient(EndPoint endPoint, DnsClientOptions? options = null)
		{
			_socket = new(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			_socket.Connect(endPoint);
			Options = options ?? new();

			new Thread(ReceiveLoop)
			{
				IsBackground = true,
				Name = "DNS Client receive thread"
			}.Start();
		}

		/// <summary>
		/// Constructor of the DNS Client
		/// </summary>
		/// <param name="address">IP address of the DNS server</param>
		/// <param name="port">Port number of the DNS server</param>
		/// <param name="options">Options of the DNS client</param>
		public DnsClient(IPAddress address, ushort port = 53, DnsClientOptions? options = null)
		{
			_socket = new(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			_socket.Connect(address, port);
			Options = options ?? new();

			new Thread(ReceiveLoop)
			{
				IsBackground = true,
				Name = "DNS Client receive thread"
			}.Start();
		}

		/// <summary>
		/// Constructor of the DNS Client
		/// </summary>
		/// <param name="address">IP address of the DNS server</param>
		/// <param name="port">Port number of the DNS server</param>
		/// <param name="options">Options of the DNS client</param>
		public DnsClient(string address, ushort port = 53, DnsClientOptions? options = null)
		{
			var addr = IPAddress.Parse(address);
			_socket = new(addr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			_socket.Connect(addr, port);
			Options = options ?? new();

			var receiveThread = new Thread(ReceiveLoop)
			{
				IsBackground = true,
				Name = "DNS Client receive thread"
			};
			receiveThread.Start();
		}
		#endregion

		#region Querying
		/// <summary>
		/// Performs a DNS query
		/// </summary>
		/// <param name="domain">Domain to query</param>
		/// <param name="qType">DNS record type to obtain</param>
		/// <returns>Result of the DNS query</returns>
		/// <exception cref="Exception">Exception thrown when DNS transaction couldn't be created.</exception>
		/// <param name="acceptTruncated">If set to true, the DNS client will accept truncated responses and won't retry the query using TCP</param>
		// ReSharper disable once MemberCanBePrivate.Global
		public async Task<DnsResponse> Query(string domain, QType qType, bool acceptTruncated = false) => await Query(new DnsQuery(domain, qType, acceptTruncated));

		/// <summary>
		/// Performs a DNS query
		/// </summary>
		/// <param name="query">DNS query to perform</param>
		/// <returns>Result of the DNS query</returns>
		/// <exception cref="Exception">Exception thrown when DNS transaction couldn't be created.</exception>
		public async Task<DnsResponse> Query(DnsQuery query)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(query.QueryLength);
			ushort transactionId = AssignTransactionId();
			bool transactionAdded = false;

			try
			{
				var status = new DnsQueryStatus(this);

				if (!_transactions.TryAdd(transactionId, status))
					throw new Exception("Couldn't create DNS transaction!");

				transactionAdded = true;
				query.BuildQuery(buffer, transactionId);

				for (ushort i = 0; i < Options.MaxAttempts; i++)
				{
					await _socket.SendAsync(new ArraySegment<byte>(buffer, 0, query.QueryLength), SocketFlags.None);
					if (await status.Wait())
						break;
				}

				status.Abort(DnsErrorCode.NoResponseFromServer);

				if (!query.AcceptTruncated && status.Response is {Truncated: true} && Options.UseTCPForTruncated)
				{
					_transactions.TryRemove(transactionId, out _);
					transactionAdded = false;

					await TCPQuery(status, buffer, query.QueryLength);
				}

				return status.Response!;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);

				if (transactionAdded)
					_transactions.TryRemove(transactionId, out _);
			}
		}

		/// <summary>
		/// Queries a PTR DNS record of an IP address
		/// </summary>
		/// <param name="address">IP address to query</param>
		/// <returns>Result of the DNS query</returns>
		// ReSharper disable once InconsistentNaming
		// ReSharper disable once UnusedMember.Global
		public async Task<DnsResponse> QueryReverseDNS(string address) => await Query(Misc.Misc.GetPtrAddress(address), QType.PTR);

		/// <summary>
        /// Queries a PTR DNS record of an IP address
        /// </summary>
        /// <param name="address">IP address to query</param>
		/// <returns>Result of the DNS query</returns>
		// ReSharper disable once InconsistentNaming
		// ReSharper disable once UnusedMember.Global
		public async Task<DnsResponse> QueryReverseDNS(IPAddress address) => await Query(Misc.Misc.GetPtrAddress(address), QType.PTR);

		// ReSharper disable once InconsistentNaming
		private async Task TCPQuery(DnsQueryStatus queryStatus, byte[] buffer, int queryLength)
		{
			var ep = Options.TCPEndpointOverride ?? _socket.RemoteEndPoint;
			Socket s = new(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			s.LingerState = new LingerOption(true, 0);
			s.ReceiveBufferSize = 2048;
			var recvBuffer = ArrayPool<byte>.Shared.Rent(2048);
			var sndBuffer = ArrayPool<byte>.Shared.Rent(queryLength + 2);

			try
			{
				s.ReceiveTimeout = (int)Options.Timeout * 2;

				sndBuffer[0] = (byte)(queryLength >> 8);
				sndBuffer[1] = (byte)(queryLength);

				Array.Copy(buffer, 0, sndBuffer, 2, queryLength);

				await s.ConnectAsync(ep);
				await s.SendAsync(new ArraySegment<byte>(sndBuffer, 0, queryLength + 2), SocketFlags.None);
				var recv = await s.ReceiveAsync(recvBuffer.AsMemory(0, 2), SocketFlags.None);

				if (recv < 2)
					return;

				var dataLength = (recvBuffer[0] << 8) | recvBuffer[1];

				if (dataLength < 12)
				{
					Options.ErrorLogging?.LogError("Received a too short TCP response.");
					return;
				}

				var timeoutCounter = 0;

				while (s.Available < dataLength)
				{
					if (timeoutCounter >= Options.Timeout)
						return;

					timeoutCounter += 30;
					await Task.Delay(30);
				}

				recv = await s.ReceiveAsync(recvBuffer.AsMemory(0, dataLength), SocketFlags.None);

				if (recv != dataLength)
					return;

				if ((recvBuffer[2] & 0x80) == 0) //Received a query, not a response
					return;

				queryStatus.Parse(recvBuffer, recv);
			}
			catch (Exception e)
			{
				Options.ErrorLogging?.LogException("Failed to make a TCP DNS query!", e);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(recvBuffer);
				ArrayPool<byte>.Shared.Return(sndBuffer);
				s.Dispose();
			}
		}
		#endregion

		#region Receiving
		private async void ReceiveLoop()
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(512);
			try
			{
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
						if (BitConverter.IsLittleEndian)
							transactionId = BinaryPrimitives.ReverseEndianness(transactionId);

						if (!_transactions.TryRemove(transactionId, out DnsQueryStatus query) || query.IsComplete)
							continue;

						query.Parse(buffer, recv);
					}
					catch (OperationCanceledException)
					{
						break;
					}
					catch (Exception e)
					{
						Options.ErrorLogging?.LogException("Failed to receive data!", e);
					}
				}
			}
			catch (Exception e)
			{
				Options.ErrorLogging?.LogException("Exception in receive loop!", e);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
				_socket.Close();
			}
		}
		#endregion

		#region Transaction IDs
		private ushort AssignTransactionId()
		{
			lock (_transactionIdLock)
			{
				return _transactionId < ushort.MaxValue
					? AbortTransactionIfExists(_transactionId++)
					: AbortTransactionIfExists(_transactionId = 0);
			}
		}

		private ushort AbortTransactionIfExists(ushort transactionId)
		{
			if (!_transactions.ContainsKey(transactionId) || !_transactions.TryRemove(transactionId, out var dnsQueryStatus))
				return transactionId;

			dnsQueryStatus.Abort(DnsErrorCode.NoResponseFromServer);
			return transactionId;
		}
		#endregion

		#region Disposal
		/// <summary>
		/// Stops and disposes the DNS client
		/// </summary>
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_ctSource.Cancel();
		}

		/// <summary>
		/// Stops and disposes the DNS client
		/// </summary>
		~DnsClient() => Dispose();
		#endregion
	}
}