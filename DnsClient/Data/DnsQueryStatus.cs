﻿using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Data.Records;
using DnsClient.Enums;

namespace DnsClient.Data
{
	internal class DnsQueryStatus
	{
		internal bool IsComplete;

		internal DnsResponse? Response;

		private readonly DnsClient _client;
		private readonly CancellationTokenSource _ctSource = new();

		internal DnsQueryStatus(DnsClient client) => _client = client;

		internal void Parse(byte[] buffer, int recv, bool tcpUsed = false)
		{
			try
			{
				byte errorCode = (byte)(buffer[3] & 0x0F);
				if (errorCode != 0) //Error returned
				{
					Abort((DnsErrorCode)errorCode);
					return;
				}

				if (buffer[4] != 0 || buffer[5] == 0)
				{
					Abort(DnsErrorCode.CantParseResponse);
					return;
				}

				ushort answers = BitConverter.ToUInt16(buffer, 6);
				if (BitConverter.IsLittleEndian)
					answers = BinaryPrimitives.ReverseEndianness(answers);

				int i = 12;

				//Ignore queries
				for (byte j = 0; j < buffer[5]; j++)
				{
					Misc.Misc.ParseDomain(buffer, i, out int read, buffer);
					i += read;
					i += 4; //Ignore query type and class
				}

				ushort processed = 0;

				Response = new DnsResponse(DnsErrorCode.NoError, (buffer[2] & 2) != 0, tcpUsed, new());

				while (i < recv && processed < answers)
				{
					int remaining = recv - i;

					if (remaining < 10)
						break;

					Misc.Misc.ParseDomain(buffer, i, out int read, buffer); //Skip query ID
					i += read;

					QType type = (QType)BitConverter.ToUInt16(buffer, i);
					if (BitConverter.IsLittleEndian)
						type = (QType)BinaryPrimitives.ReverseEndianness((ushort)type);

					i += 4; //Type processed above, skip class

					uint ttl = BitConverter.ToUInt32(buffer, i);
					if (BitConverter.IsLittleEndian)
						ttl = BinaryPrimitives.ReverseEndianness(ttl);

					i += 4; //TTL Processed

					int length = BitConverter.ToUInt16(buffer, i);
					if (BitConverter.IsLittleEndian)
						length = BinaryPrimitives.ReverseEndianness((ushort)length);

					i += 2; //Length processed

					if (remaining < length)
						break;

					DnsRecord.DNSRecord? record = DnsRecord.Parse(type, new ArraySegment<byte>(buffer, i, length), ttl, buffer);
					if (record != null)
						Response.Records!.Add(record);

					i += length;
					processed++;
				}

				Abort();
			}
			catch (Exception e)
			{
				_client.Options.ErrorLogging?.LogException("Failed to parse response!", e);
				Abort(DnsErrorCode.CantParseResponse);
			}
		}

		private void Abort()
		{
			if (IsComplete)
				return;

			IsComplete = true;
			_ctSource.Cancel();
		}

		internal void Abort(DnsErrorCode errorCode)
		{
			if (IsComplete)
				return;

			IsComplete = true;
			Response = new DnsResponse(errorCode);
			_ctSource.Cancel();
		}

		internal async Task<bool> Wait()
		{
			try
			{
				CancellationToken ct = _ctSource.Token;
				int timeWaited = 0;

				while (!IsComplete && !ct.IsCancellationRequested)
				{
					await Task.Delay(_client.Options.TimeoutInnerDelay, ct);

					if ((timeWaited += _client.Options.TimeoutInnerDelay) >= _client.Options.Timeout)
						return false;
				}

				return true;
			}
			catch (OperationCanceledException)
			{
				//Ignore
				return true;
			}
		}
	}
}