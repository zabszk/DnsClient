namespace DnsClient.Enums;

/// <summary>
/// Error codes of the query
/// </summary>
public enum DnsErrorCode : byte
{
	/// <summary>
	/// No error returned
	/// </summary>
	NoError = 0,

	/// <summary>
	/// The DNS server was unable to interpret the query
	/// </summary>
	FormatError = 1,

	/// <summary>
	/// The DNS server was unable to process this query due to a problem with the DNS server
	/// </summary>
	ServerFailure = 2,

	/// <summary>
	/// Meaningful only for responses from an authoritative DNS server, this code signifies that the domain name referenced in the query does ot exist
	/// </summary>
	NameError = 3,

	/// <summary>
	/// The DNS server does not support the requested kind of query
	/// </summary>
	NotImplemented = 4,

	/// <summary>
	/// The DNS server refuses to perform the specified operation for policy reasons
	/// </summary>
	Refused = 5,

	/// <summary>
	/// The DNS server did not respond to the query
	/// </summary>
	NoResponseFromServer = 6,

	/// <summary>
	/// Response from the DNS server couldn't be parsed
	/// </summary>
	CantParseResponse = 7
}