using DnsClient;
using DnsClient.Data;
using DnsClient.Enums;
using DnsClient.Logging;
using DnsClient.Misc;

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
	if (string.IsNullOrWhiteSpace(domain))
	{
		Console.WriteLine();
		continue;
	}

	var sp = type!.ToUpperInvariant().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
	QType[] types = new QType[sp.Length];
	int lastType = 0;

	if (sp.Length == 1 && sp.Contains("PTR") && Misc.TryGetPtrAddress(domain, out var ptrAddress))
	{
		domain = ptrAddress;
		Console.WriteLine($"Changed queried domain to: {domain}");
	}

	foreach (var s in sp)
	{
		if (!Enum.TryParse(typeof(QType), s, true, out var qType))
		{
			Console.WriteLine($"Invalid record type {s}!");
			Console.WriteLine();
			continue;
		}

		types[lastType++] = (QType)qType;
	}

	Console.WriteLine("Querying 1.1.1.1...");
	DnsResponse response = await dns.Query(new DnsQuery(domain!, types));

	Console.WriteLine($"Query result: {response.ErrorCode}");

	if (response.Truncated)
		Console.WriteLine("Result is truncated by the DNS server! It does not contain all the existing records!");

	if (response.TCPUsed)
		Console.WriteLine("TCP was used to perform this query.");

	if (response.Records == null)
		Console.WriteLine("No records found");
	else
	{
		Console.WriteLine($"Records found: {response.Records.Count}");

		foreach (var record in response.Records)
			Console.WriteLine(" - " + record);
	}

	Console.WriteLine();
}