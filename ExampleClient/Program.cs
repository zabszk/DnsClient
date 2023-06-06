using DnsClient;
using DnsClient.Data;
using DnsClient.Enums;
using DnsClient.Logging;

using DnsClient.DnsClient dns = new("1.1.1.1", options: new DnsClientOptions
{
	ErrorLogging = new StdoutLogger()
});

while (true)
{
	Console.Write("Domain: ");
	var domain = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(domain))
	{
		Console.WriteLine();
		continue;
	}

	if (domain.Equals("exit", StringComparison.OrdinalIgnoreCase))
		break;

	Console.Write("Record type: ");
	var type = Console.ReadLine();
	if (string.IsNullOrWhiteSpace(domain) || !Enum.TryParse(typeof(QType), type, true, out var QType))
	{
		Console.WriteLine("Invalid record type!");
		Console.WriteLine();
		continue;
	}

	Console.WriteLine("Querying 1.1.1.1...");
	DnsResponse response = await dns.Query(new DnsQuery(domain, (QType)QType));

	Console.WriteLine($"Query result: {response.ErrorCode}");
	Console.WriteLine($"Records found: {response.Records.Count}");

	foreach (var record in response.Records)
		Console.WriteLine(" - " + record);

	Console.WriteLine();
}