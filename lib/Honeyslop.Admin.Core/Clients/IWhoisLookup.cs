using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Core.Clients;

public interface IWhoisLookup
{
    Task<WhoisDto> LookupAsync(string clientIp, CancellationToken cancellationToken);
}
