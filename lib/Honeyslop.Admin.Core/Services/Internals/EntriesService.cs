using System.Text.RegularExpressions;
using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Extensions;
using Honeyslop.Common;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Llm.Contracts;

namespace Honeyslop.Admin.Core.Services.Internals;

internal sealed partial class EntriesService(
    IEntryRepository entryRepository,
    IGenerationOutputStore outputStore,
    ICountryLookup countryLookup,
    IAccessGuard access,
    IServerTimeRepository serverTime) : IEntriesService
{
    private const string DefaultContentType = "text/html; charset=utf-8";

    public async Task<EntriesPageDto> GetPageAsync(EntriesQuery query, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var page = Math.Max(0, query.Page);
        var asOf = await serverTime.GetNowAsync(cancellationToken);
        var (host, target, state) = query.Filter;
        var (order, direction) = query.Sort.ToOrder();
        var totals = await entryRepository.GetTotalsAsync(cancellationToken);
        var total = await entryRepository.CountAsync(host, target, state, cancellationToken);
        var entries = await entryRepository.GetPageAsync(host, target, state, order, direction, page, IEntriesService.PageSize, cancellationToken);
        return new EntriesPageDto(totals, new PagedDto<Entry>(entries, total, page, IEntriesService.PageSize), asOf);
    }

    public async Task<EntryDetailsDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var entry = await entryRepository.GetByIdOrDefaultAsync(id, cancellationToken);
        if (entry is null)
            return null;
        var current = entry.Status == EntryStatus.Expired
            ? await entryRepository.GetByKeyHashOrDefaultAsync(entry.KeyHash, cancellationToken)
            : null;
        return new EntryDetailsDto(
            entry,
            entry.ClientIp is null ? null : countryLookup.Find(entry.ClientIp),
            current?.Id,
            entry.State == EntryState.Generating ? outputStore.GetOrDefault(entry.Id) : null,
            await serverTime.GetNowAsync(cancellationToken));
    }

    public async Task<GenerationOutputDto?> GetOutputAsync(long id, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        return outputStore.GetOrDefault(id);
    }

    public async Task<string?> GetPreviewAsync(long id, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        return (await entryRepository.GetByIdOrDefaultAsync(id, cancellationToken))?.ResponseBody;
    }

    public async Task<int> ExpireAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        return ids.Count > 0
            ? await entryRepository.ExpireAsync(ids, await serverTime.GetNowAsync(cancellationToken), cancellationToken)
            : 0;
    }

    public async Task<int> ExpireMatchingAsync(EntryFilter filter, CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        return await entryRepository.ExpireMatchingAsync(filter.Host, filter.Target, filter.State, await serverTime.GetNowAsync(cancellationToken), cancellationToken);
    }

    public async Task<EntryChangeDto> RegenerateAsync(
        IReadOnlyCollection<long> ids,
        string? guidance,
        CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        var note = string.IsNullOrWhiteSpace(guidance) ? null : guidance.Trim();
        if (note?.Length > Entry.GuidanceMaxLength)
            return Refused(nameof(Entry.Guidance), $"Must be at most {Entry.GuidanceMaxLength} characters.");
        return await ReplaceWithAsync(ids, (current, now) => Queued(current, note, now), cancellationToken);
    }

    public async Task<EntryChangeDto> ReplaceAsync(
        IReadOnlyCollection<long> ids,
        EntryResponseDto response,
        CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        var errors = Validate(response);
        if (errors.Count > 0)
            return new EntryChangeDto(errors, []);
        var headers = WithContentType(response.Headers);
        return await ReplaceWithAsync(
            ids,
            (current, now) => Written(current, response.Status, headers, response.Body, now),
            cancellationToken);
    }

    private async Task<EntryChangeDto> ReplaceWithAsync(
        IReadOnlyCollection<long> ids,
        Func<Entry, DateTimeOffset, Entry> replace,
        CancellationToken cancellationToken)
    {
        var current = ids.Count > 0 ? await entryRepository.GetActiveAsync(ids, cancellationToken) : [];
        if (current.Length == 0)
            return Refused("Entries", "None of these entries is active any more.");
        var now = await serverTime.GetNowAsync(cancellationToken);
        Entry[] replacements = [.. current.Select(x => replace(x, now))];
        if (!await entryRepository.TryReplaceAsync(replacements, now, cancellationToken))
            return Refused("Entries", "Another change to these entries got there first, reload them and try again.");
        return new EntryChangeDto(new Dictionary<string, string>(), [.. replacements.Select(x => x.Id)]);
    }

    private Entry Queued(Entry current, string? guidance, DateTimeOffset now)
    {
        var entry = Copy(current, now);
        entry.Status = EntryStatus.Pending;
        entry.Guidance = guidance;
        return entry;
    }

    private Entry Written(Entry current, int status, IReadOnlyList<ResponseHeader> headers, string body, DateTimeOffset now)
    {
        var entry = Copy(current, now);
        entry.Status = EntryStatus.Ready;
        entry.ResponseStatus = status;
        entry.ResponseHeaders = [.. headers.Select(x => new ResponseHeader { Name = x.Name, Value = x.Value })];
        entry.ResponseBody = body;
        entry.GeneratedAt = now;
        return entry;
    }

    private Entry Copy(Entry current, DateTimeOffset now)
    {
        return new Entry
        {
            KeyHash = current.KeyHash,
            Method = current.Method,
            Host = current.Host,
            Target = current.Target,
            ClientIp = current.ClientIp,
            UserAgent = current.UserAgent,
            RequestHeaders = current.RequestHeaders,
            RequestBody = current.RequestBody,
            ChangedBy = access.Viewer.Name,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Dictionary<string, string> Validate(EntryResponseDto response)
    {
        var errors = new Dictionary<string, string>();
        if (response.Status is < 200 or > 599)
            errors[nameof(response.Status)] = "Must be from 200 to 599.";
        if (HeaderProblem(response.Headers) is { } problem)
            errors[nameof(response.Headers)] = problem;
        if (response.Body.Length > Entry.ResponseBodyMaxLength)
            errors[nameof(response.Body)] = $"Must be at most {Entry.ResponseBodyMaxLength} characters.";
        return errors;
    }

    private static string? HeaderProblem(IReadOnlyCollection<ResponseHeader> headers)
    {
        if (headers.Count > Entry.ResponseHeadersMaxCount)
            return $"At most {Entry.ResponseHeadersMaxCount} headers.";
        foreach (var header in headers)
        {
            if (header.Name.Length > ResponseHeader.NameMaxLength || !HeaderName().IsMatch(header.Name))
                return $"\"{header.Name}\" is not a header name.";
            if (ReservedResponseHeaders.Contains(header.Name))
                return $"{header.Name} is set by the server.";
            if (header.Value.Length > ResponseHeader.ValueMaxLength || header.Value.Any(x => x != '\t' && char.IsControl(x)))
                return $"The value of {header.Name} must be printable and at most {ResponseHeader.ValueMaxLength} characters.";
        }

        return null;
    }

    private static IReadOnlyList<ResponseHeader> WithContentType(ResponseHeader[] headers)
    {
        return headers.Any(x => x.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            ? headers
            : [.. headers, new ResponseHeader { Name = "Content-Type", Value = DefaultContentType }];
    }

    private static EntryChangeDto Refused(string field, string error)
    {
        return new EntryChangeDto(new Dictionary<string, string> { [field] = error }, []);
    }

    [GeneratedRegex(@"^[!#$%&'*+\-.^_`|~0-9A-Za-z]+$")]
    private static partial Regex HeaderName();

}
