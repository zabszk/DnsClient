using System;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient.Structs
{
	internal class DnsQueryStatus
	{
		internal Status State = Status.InProgress;

		internal bool IsComplete => State != Status.InProgress;

		internal CancellationTokenSource? CtSource = new();
		internal DnsResponse? Response = null;

		private readonly DnsClient _client;

		internal DnsQueryStatus(DnsClient client) => _client = client;

		internal void Abort(bool success)
		{
			if (IsComplete)
				return;

			State = success ? Status.Completed : Status.Failed;
			CtSource?.Cancel();
		}

		internal async Task<bool> Wait()
		{
			if (CtSource == null)
				return false;

			try
			{
				CancellationToken ct = CtSource.Token;
				int timeWaited = 0;

				while (!IsComplete && !ct.IsCancellationRequested)
				{
					await Task.Delay(_client.Options.TimeoutInnerDelay, ct);

					if ((timeWaited += _client.Options.TimeoutInnerDelay) >= _client.Options.Timeout)
						return false;
				}

				return true;
			}
			catch (TaskCanceledException)
			{
				//Ignore
				return false;
			}
		}

		internal enum Status
		{
			InProgress,
			Completed,
			Failed
		}
	}
}