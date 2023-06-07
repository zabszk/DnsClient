# DnsClient
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

# Basic usage
```cs
using DnsClient.DnsClient dns = new("1.1.1.1", options: new DnsClientOptions
{
  ErrorLogging = new StdoutLogger()
});

DnsResponse response = await dns.Query("google.com", QType.AAAA);
```
