namespace Honeyslop.Admin.Clients;

public interface IUserAgentProvider
{
    Task<string> GetAsync(CancellationToken cancellationToken);
}
