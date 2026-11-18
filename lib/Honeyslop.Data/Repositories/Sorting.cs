namespace Honeyslop.Data.Repositories;

public enum SortDirection
{
    Asc,
    Desc
}

public sealed record Sorting<TColumn>(TColumn Column, SortDirection Direction)
    where TColumn : struct, Enum;

public enum EntryColumn
{
    Status,
    Method,
    Host,
    Path,
    Code,
    Size,
    Hits,
    Created,
    LastHit
}

public enum RequestColumn
{
    Time,
    Client,
    Method,
    Host,
    Path,
    Outcome,
    Code,
    Duration,
    UserAgent
}

public enum ClientColumn
{
    Client,
    Requests,
    Hosts,
    Paths,
    FirstSeen,
    LastSeen,
    UserAgent
}

public enum ReportColumn
{
    Requested,
    Client,
    By,
    To,
    Status,
    Sent,
    Requests,
    Attempts,
    Error
}
