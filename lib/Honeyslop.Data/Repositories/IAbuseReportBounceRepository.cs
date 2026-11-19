using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public interface IAbuseReportBounceRepository
{
    Task<int> AddAsync(IReadOnlyCollection<AbuseReportBounce> bounces, CancellationToken cancellationToken);
    Task<AbuseReportBounce[]> GetByReportAsync(long reportId, CancellationToken cancellationToken);
    Task<BouncedContact[]> GetContactsAsync(IReadOnlyCollection<string> recipients, CancellationToken cancellationToken);
    Task<BouncedContact[]> GetContactsPageAsync(DateTimeOffset since, string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountContactsAsync(DateTimeOffset since, string? search, CancellationToken cancellationToken);
}