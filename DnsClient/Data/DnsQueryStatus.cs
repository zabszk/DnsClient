using System.Threading;
using System.Threading.Tasks;
using DnsClient.Enums;

namespace DnsClient.Data
{
	internal class DnsQueryStatus
	{
		internal bool IsComplete;

		internal DnsResponse? Response;

		private readonly DnsClient _client;
		private readonly CancellationTokenSource? _ctSource = new();

		internal DnsQueryStatus(DnsClient client) => _client = client;

		internal void Abort(DnsErrorCode errorCode)
		{
			if (IsComplete)
				return;

			IsComplete = true;
			Response = new DnsResponse(errorCode);
			_ctSource?.Cancel();
		}

		internal async Task<bool> Wait()
		{
			if (_ctSource == null)
				return false;

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
			catch (TaskCanceledException)
			{
				//Ignore
				return false;
			}
		}
	}
}