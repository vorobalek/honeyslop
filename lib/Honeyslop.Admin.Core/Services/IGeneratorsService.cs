using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Core.Services;

public interface IGeneratorsService
{
    const int GeneratingLimit = 50;
    const int QueuedLimit = 20;

    static readonly TimeSpan ProductionWindow = TimeSpan.FromHours(24);

    Task<GeneratorsDto> GetOverviewAsync(CancellationToken cancellationToken);
}
