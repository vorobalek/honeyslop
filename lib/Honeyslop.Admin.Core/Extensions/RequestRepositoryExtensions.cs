using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Core.Extensions;

public static class RequestRepositoryExtensions
{
    extension(IRequestRepository repository)
    {
        public Task<RequestRecord[]> GetPageAsync(
            RequestFilter filter,
            Sorting<RequestColumn>? sort,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var (order, direction) = sort.ToOrder();
            return repository.GetPageAsync(
                filter.ClientIp,
                filter.Host,
                filter.Target,
                filter.Outcome,
                filter.EntryId,
                filter.Method,
                filter.UserAgent,
                filter.Since,
                filter.Until,
                order,
                direction,
                pageNumber,
                pageSize,
                cancellationToken);
        }

        public Task<int> CountAsync(RequestFilter filter, CancellationToken cancellationToken)
        {
            return repository.CountAsync(
                filter.ClientIp,
                filter.Host,
                filter.Target,
                filter.Outcome,
                filter.EntryId,
                filter.Method,
                filter.UserAgent,
                filter.Since,
                filter.Until,
                filter.AfterId,
                cancellationToken);
        }

        public Task<RequestBucket[]> GetBreakdownAsync(RequestFilter filter, RequestDimension dimension, int top, CancellationToken cancellationToken)
        {
            return repository.GetBreakdownAsync(
                filter.ClientIp,
                filter.Host,
                filter.Target,
                filter.Outcome,
                filter.EntryId,
                filter.Method,
                filter.UserAgent,
                filter.Since,
                filter.Until,
                dimension,
                top,
                cancellationToken);
        }
    }
}
