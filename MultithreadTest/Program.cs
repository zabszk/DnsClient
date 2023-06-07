using DnsClient;
using DnsClient.Enums;
using DnsClient.Logging;

using DnsClient.DnsClient dns = new("1.1.1.1", options: new DnsClientOptions
{
	ErrorLogging = new StdoutLogger()
});

string[] domains = new[] {"google.com", "gmail.com", "cloudflare.com", "github.com"};

foreach (var domain in domains)
#pragma warning disable CS4014
	Task.Run(() => Query(domain, QType.A));
#pragma warning restore CS4014

while (true)
	await Task.Delay(60000);

async Task Query(string domain, QType qType)
{
	var rnd = new Random();

	while (true)
	{
		Console.WriteLine($"Resolving {domain}...");
		var response = dns.Query(domain, qType);

		if (response.Result.Records == null || response.Result.Records.Count == 0)
			Console.WriteLine($"Resolved {domain} with result {response.Result}!");
		else Console.WriteLine($"Resolved {domain} to {response.Result.Records[0]}!");

		await Task.Delay(rnd.Next(40, 200));
	}
	// ReSharper disable once FunctionNeverReturns
}
