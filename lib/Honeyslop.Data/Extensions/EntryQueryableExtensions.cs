using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Extensions;

internal static class EntryQueryableExtensions
{
    private const string LikeEscape = "\\";

    extension(IQueryable<Entry> query)
    {
        public IQueryable<Entry> FilterBy(EntryFilter filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Host))
            {
                var pattern = filter.Host.Trim().ContainsPattern;
                query = query.Where(x => EF.Functions.ILike(x.Host, pattern, LikeEscape));
            }

            if (!string.IsNullOrWhiteSpace(filter.Target))
            {
                var pattern = filter.Target.Trim().ContainsPattern;
                query = query.Where(x => EF.Functions.ILike(x.Target, pattern, LikeEscape));
            }

            query = filter.State switch
            {
                EntryState.Queued => query.Where(x => x.Status == EntryStatus.Pending && x.ClaimedBy == null),
                EntryState.Generating => query.Where(x => x.Status == EntryStatus.Pending && x.ClaimedBy != null),
                EntryState.Ready => query.Where(x => x.Status == EntryStatus.Ready),
                EntryState.Failed => query.Where(x => x.Status == EntryStatus.Failed),
                EntryState.Expired => query.Where(x => x.Status == EntryStatus.Expired),
                _ => query.Where(x => x.Status != EntryStatus.Expired)
            };

            return query;
        }

        public Task<int> ExpireAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            return query
                .Where(x => x.Status != EntryStatus.Expired)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Status, EntryStatus.Expired)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
        }
    }

    extension(IQueryable<RequestRecord> query)
    {
        public IQueryable<RequestRecord> FilterBy(RequestFilter filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.ClientIp))
            {
                var clientIp = filter.ClientIp.Trim();
                query = query.Where(x => x.ClientIp == clientIp);
            }

            if (!string.IsNullOrWhiteSpace(filter.Host))
            {
                var pattern = filter.Host.Trim().ContainsPattern;
                query = query.Where(x => EF.Functions.ILike(x.Host, pattern, LikeEscape));
            }

            if (!string.IsNullOrWhiteSpace(filter.Target))
            {
                var pattern = filter.Target.Trim().ContainsPattern;
                query = query.Where(x => EF.Functions.ILike(x.Target, pattern, LikeEscape));
            }

            if (filter.Outcome is { } outcome)
                query = query.Where(x => x.Outcome == outcome);

            if (filter.EntryId is { } entryId)
                query = query.Where(x => x.EntryId == entryId);

            return query;
        }
    }

    extension(string value)
    {
        private string ContainsPattern =>
            "%" +
            value
                .Replace(LikeEscape, LikeEscape + LikeEscape)
                .Replace("%", LikeEscape + "%")
                .Replace("_", LikeEscape + "_") +
            "%";
    }
}
