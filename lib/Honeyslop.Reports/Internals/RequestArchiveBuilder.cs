using Honeyslop.Data.Repositories;
using Honeyslop.Reports.Contracts;

namespace Honeyslop.Reports.Internals;

internal sealed class RequestArchiveBuilder(IRequestRepository requestRepository) : IRequestArchiveBuilder
{
    private const int BatchSize = 1000;

    public async Task<RequestLogArchive?> BuildAsync(string clientIp, int maxRequests, CancellationToken cancellationToken)
    {
        var total = await requestRepository.CountAsync(clientIp, null, null, null, null, null, null, null, null, null, cancellationToken);
        if (total == 0 || await requestRepository.GetLogStartAsync(clientIp, maxRequests, cancellationToken) is not { } fromId)
            return null;

        var included = Math.Min(total, maxRequests);
        var lastRequestId = 0L;
        using var archive = new RequestArchive(clientIp, total, included);
        while (archive.Count < included)
        {
            var batch = await requestRepository.GetLogAsync(clientIp, fromId, Math.Min(BatchSize, included - archive.Count), cancellationToken);
            if (batch.Length == 0)
                break;
            foreach (var request in batch)
                archive.Add(request);
            lastRequestId = batch[^1].Id;
            fromId = lastRequestId + 1;
        }

        return archive.Count == 0 ? null : new RequestLogArchive(archive.ToArray(), archive.Count, total, lastRequestId);
    }
}