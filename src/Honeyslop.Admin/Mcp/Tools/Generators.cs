using System.ComponentModel;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using ModelContextProtocol.Server;

namespace Honeyslop.Admin.Mcp.Tools;

[McpServerToolType]
public sealed class Generators(IGeneratorsService generatorsService)
{
    [McpServerTool(Name = "get_generators", Title = "Get the generators", ReadOnly = true, Idempotent = true, OpenWorld = true)]
    [Description("The model servers with their slots, speed and the last 24 hours of production, the entries being generated now and the queue.")]
    public Task<GeneratorsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        return generatorsService.GetOverviewAsync(cancellationToken);
    }
}
