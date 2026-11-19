using System.Text.Json;

namespace Honeyslop.Admin.Core.Clients;

public interface IRipeStatClient
{
    Task<JsonElement> GetAsync(string dataCall, string resource, CancellationToken cancellationToken);
}