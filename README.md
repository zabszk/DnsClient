# DnsClient [![GitHub release](https://flat.badgen.net/github/release/zabszk/DnsClient)](https://github.com/zabszk/DnsClient/releases/) [![NuGet](https://flat.badgen.net/nuget/v/zabszk.DnsClient/latest)](https://www.nuget.org/packages/zabszk.DnsClient/) [![License](https://flat.badgen.net/github/license/zabszk/DnsClient)](https://github.com/zabszk/DnsClient/blob/master/LICENSE)
A DNS Client library for C#.

DNS Client class provided by this library is thread-safe.

# Supported DNS records types
 * A
 * AAAA
 * CNAME
 * MX
 * SRV
 * TXT
 * NS
 * PTR
 * SOA
 * DS
 * DNSKEY
 * CAA
 * URI

# Basic usage
```cs
using DnsClient.DnsClient dns = new("1.1.1.1", options: new DnsClientOptions
{
  ErrorLogging = new StdoutLogger()
});

DnsResponse response = await dns.Query("google.com", QType.AAAA);
```
