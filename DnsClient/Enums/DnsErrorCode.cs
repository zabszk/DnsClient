namespace DnsClient.Enums;

public enum DnsErrorCode : byte
{
	NoError = 0,
	FormatError = 1,
	ServerFailure = 2,
	NameError = 3,
	NotImplemented = 4,
	Refused = 5,
	NoResponseFromServer = 6,
	CantParseResponse = 7
}