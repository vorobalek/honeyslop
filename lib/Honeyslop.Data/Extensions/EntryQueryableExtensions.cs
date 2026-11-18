using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Extensions;

internal static class EntryQueryableExtensions
{
    private const string LikeEscape = "\\";

    extension(IQueryable<Entry> query)
    {
        public IQueryable<Entry> FilterBy(string? host, string? target, EntryState? state)
        {
            if (!string.IsNullOrWhiteSpace(host))
            {
                var pattern = host.Trim().ContainsPattern;
                query = query.Where(x => EF.Functions.ILike(x.Host, pattern, LikeEscape));
            }

            if (!string.IsNullOrWhiteSpace(target))
            {
                var pattern = target.Trim().ContainsPattern;
                query = query.Where(x => EF.Functions.ILike(x.Target, pattern, LikeEscape));
            }

            query = state switch
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
        public IQueryable<RequestRecord> FilterBy(
            string? clientIp,
            string? host,
            string? target,
            RequestOutcome? outcome,
            long? entryId,
            long? afterId,
            string? method,
            string? userAgent,
            DateTimeOffset? since,
            DateTimeOffset? until)
        {
            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                var exactIp = clientIp.Trim();
                query = query.Where(x => x.ClientIp == exactIp);
            }

            if (!string.IsNullOrWhiteSpace(host))
            {
                var pattern = host.Trim().ContainsPattern;
                query = query.Where(x => EF.Functions.ILike(x.Host, pattern, LikeEscape));
            }

            if (!string.IsNullOrWhiteSpace(target))
            {
                var pattern = target.Trim().ContainsPattern;
                query = query.Where(x => EF.Functions.ILike(x.Target, pattern, LikeEscape));
            }

            if (outcome is not null)
                query = query.Where(x => x.Outcome == outcome);

            if (entryId is not null)
                query = query.Where(x => x.EntryId == entryId);

            if (afterId is not null)
                query = query.Where(x => x.Id > afterId);

            if (!string.IsNullOrWhiteSpace(method))
            {
                var upperMethod = method.Trim().ToUpperInvariant();
                query = query.Where(x => x.Method == upperMethod);
            }

            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                var pattern = userAgent.Trim().ContainsPattern;
                query = query.Where(x => x.UserAgent != null && EF.Functions.ILike(x.UserAgent, pattern, LikeEscape));
            }

            if (since is not null)
                query = query.Where(x => x.ReceivedAt >= since);

            if (until is not null)
                query = query.Where(x => x.ReceivedAt < until);

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
