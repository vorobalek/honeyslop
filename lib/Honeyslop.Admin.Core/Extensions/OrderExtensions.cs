using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Extensions;

public static class OrderExtensions
{
    extension(Sorting<EntryColumn>? sort)
    {
        public (EntryOrder Order, SortDirection Direction) ToOrder()
        {
            return sort is null
                ? (EntryOrder.Id, SortDirection.Desc)
                : (sort.Column switch
                {
                    EntryColumn.Status => EntryOrder.Status,
                    EntryColumn.Method => EntryOrder.Method,
                    EntryColumn.Host => EntryOrder.Host,
                    EntryColumn.Path => EntryOrder.Target,
                    EntryColumn.Code => EntryOrder.ResponseStatus,
                    EntryColumn.Size => EntryOrder.ResponseSize,
                    EntryColumn.Hits => EntryOrder.Hits,
                    EntryColumn.LastHit => EntryOrder.LastHitAt,
                    _ => EntryOrder.Id
                }, sort.Direction);
        }
    }

    extension(Sorting<RequestColumn>? sort)
    {
        public (RequestOrder Order, SortDirection Direction) ToOrder()
        {
            return sort is null
                ? (RequestOrder.Id, SortDirection.Desc)
                : (sort.Column switch
                {
                    RequestColumn.Client => RequestOrder.ClientIp,
                    RequestColumn.Method => RequestOrder.Method,
                    RequestColumn.Host => RequestOrder.Host,
                    RequestColumn.Path => RequestOrder.Target,
                    RequestColumn.Outcome => RequestOrder.Outcome,
                    RequestColumn.Code => RequestOrder.ResponseStatus,
                    RequestColumn.Duration => RequestOrder.DurationMs,
                    RequestColumn.UserAgent => RequestOrder.UserAgent,
                    _ => RequestOrder.Id
                }, sort.Direction);
        }
    }

    extension(Sorting<ClientColumn>? sort)
    {
        public (ClientOrder Order, SortDirection Direction) ToOrder()
        {
            return sort is null
                ? (ClientOrder.LastSeenAt, SortDirection.Desc)
                : (sort.Column switch
                {
                    ClientColumn.Client => ClientOrder.ClientIp,
                    ClientColumn.Requests => ClientOrder.Requests,
                    ClientColumn.Hosts => ClientOrder.Hosts,
                    ClientColumn.Paths => ClientOrder.Targets,
                    ClientColumn.FirstSeen => ClientOrder.FirstSeenAt,
                    ClientColumn.UserAgent => ClientOrder.LastUserAgent,
                    _ => ClientOrder.LastSeenAt
                }, sort.Direction);
        }
    }

    extension(Sorting<ReportColumn>? sort)
    {
        public (ReportOrder Order, SortDirection Direction) ToOrder()
        {
            return sort is null
                ? (ReportOrder.Id, SortDirection.Desc)
                : (sort.Column switch
                {
                    ReportColumn.Client => ReportOrder.ClientIp,
                    ReportColumn.By => ReportOrder.RequestedBy,
                    ReportColumn.To => ReportOrder.Recipient,
                    ReportColumn.Status => ReportOrder.Status,
                    ReportColumn.Sent => ReportOrder.SentAt,
                    ReportColumn.Requests => ReportOrder.AttachedRequests,
                    ReportColumn.Attempts => ReportOrder.Attempts,
                    ReportColumn.Error => ReportOrder.Error,
                    _ => ReportOrder.Id
                }, sort.Direction);
        }
    }
}