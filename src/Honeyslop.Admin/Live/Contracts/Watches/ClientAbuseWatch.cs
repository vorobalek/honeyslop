namespace Honeyslop.Admin.Live.Contracts.Watches;

public sealed record ClientAbuseWatch(string Ip) : WatchRequest;
