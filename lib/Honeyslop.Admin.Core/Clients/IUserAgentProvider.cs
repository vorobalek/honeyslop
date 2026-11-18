namespace Honeyslop.Admin.Core.Clients;

public interface IUserAgentProvider
{
    Task<string> GetAsync(CancellationToken cancellationToken);
}
