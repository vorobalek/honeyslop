namespace Honeyslop.Admin.Clients;

public interface IWhoisLookup
{
    Task<WhoisInfo> LookupAsync(string clientIp, CancellationToken cancellationToken);
}
