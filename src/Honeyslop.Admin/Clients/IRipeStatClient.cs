using System.Text.Json;

namespace Honeyslop.Admin.Clients;

public interface IRipeStatClient
{
    Task<JsonElement> GetAsync(string dataCall, string resource, CancellationToken cancellationToken);
}
