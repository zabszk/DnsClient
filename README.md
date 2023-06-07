# DnsClient
A DNS Client library for C#.

# Supported DNS record Types
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

# Basic usage
```cs
using DnsClient.DnsClient dns = new("1.1.1.1", options: new DnsClientOptions
{
  ErrorLogging = new StdoutLogger()
});

DnsResponse response = await dns.Query("google.com", QType.AAAA);
```
