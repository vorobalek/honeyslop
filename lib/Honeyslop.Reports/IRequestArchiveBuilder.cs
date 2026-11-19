using Honeyslop.Reports.Contracts;

namespace Honeyslop.Reports;

public interface IRequestArchiveBuilder
{
    Task<RequestLogArchive?> BuildAsync(string clientIp, int maxRequests, CancellationToken cancellationToken);
}