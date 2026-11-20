using System.Text.RegularExpressions;
using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Extensions;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Common;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Data.Requests;
using Honeyslop.Llm.Contracts;
using Microsoft.Extensions.Options;

namespace Honeyslop.Admin.Core.Services.Internals;

internal sealed partial class EntriesService(
    IEntryRepository entryRepository,
    IGenerationOutputStore outputStore,
    ICountryLookup countryLookup,
    IAccessGuard access,
    IServerTimeRepository serverTime,
    ISettingsRepository settingsRepository,
    IOptions<RequestRules> requestRules,
    IOptions<ResponseDelayOptions> responseDelayOptions) : IEntriesService
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
        if (!access.Viewer.CanOperate)
        {
            entry.ChangedBy = null;
            entry.Guidance = null;
        }

        var current = entry.Status == EntryStatus.Expired
            ? await entryRepository.GetByKeyHashOrDefaultAsync(entry.KeyHash, cancellationToken)
            : null;
        var settings = (await settingsRepository.GetAsync(null, cancellationToken)).Honeyslop;
        return new EntryDetailsDto(
            entry,
            entry.ClientIp is null ? null : countryLookup.Find(entry.ClientIp),
            current?.Id,
            entry.State == EntryState.Generating ? outputStore.GetOrDefault(entry.Id) : null,
            settings.ResponseDelayMilliseconds ?? responseDelayOptions.Value.DefaultMilliseconds,
            settings.NoiseEnabled,
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

    public async Task<EntryChangeDto> CreateAsync(
        EntryRequestDto request,
        string? guidance,
        CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        var errors = new Dictionary<string, string>();
        var template = ReadRequest(request, errors);
        var note = string.IsNullOrWhiteSpace(guidance) ? null : guidance.Trim();
        if (note?.Length > Entry.GuidanceMaxLength)
            errors[nameof(Entry.Guidance)] = $"Must be at most {Entry.GuidanceMaxLength} characters.";
        if (template is null || errors.Count > 0)
            return new EntryChangeDto(errors, []);
        return await CreateWithAsync(template, (current, now) => Queued(current, note, now), cancellationToken);
    }

    public async Task<EntryChangeDto> CreateWithResponseAsync(
        EntryRequestDto request,
        EntryResponseDto response,
        CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        var errors = Validate(response);
        var template = ReadRequest(request, errors);
        if (template is null || errors.Count > 0)
            return new EntryChangeDto(errors, []);
        var headers = WithContentType(response.Headers);
        return await CreateWithAsync(
            template,
            (current, now) => Written(current, response.Status, headers, response.Body, now),
            cancellationToken);
    }

    public async Task<EntryChangeDto> SetResponseDelayAsync(
        IReadOnlyCollection<long> ids,
        int? milliseconds,
        CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        if (milliseconds is < 0 or > Entry.ResponseDelayMaxMilliseconds)
            return Refused(nameof(Entry.ResponseDelayMilliseconds), $"Must be from 0 to {Entry.ResponseDelayMaxMilliseconds} milliseconds.");
        var changed = ids.Count > 0
            ? await entryRepository.SetResponseDelayAsync(ids, milliseconds, await serverTime.GetNowAsync(cancellationToken), cancellationToken)
            : [];
        return changed.Length > 0
            ? new EntryChangeDto(new Dictionary<string, string>(), changed)
            : Refused("Entries", "None of these entries is served any more.");
    }

    public async Task<EntryChangeDto> SetNoiseAsync(
        IReadOnlyCollection<long> ids,
        NoiseTier? tier,
        CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        if (tier is { } chosen && !Enum.IsDefined(chosen))
            return Refused(nameof(Entry.Noise), "Must be tier 1, 2 or 3.");
        if (tier is not null && !(await settingsRepository.GetAsync(null, cancellationToken)).Honeyslop.NoiseEnabled)
            return Refused(nameof(Entry.Noise), "Noise defence is off in the settings.");
        var changed = ids.Count > 0
            ? await entryRepository.SetNoiseAsync(ids, tier, await serverTime.GetNowAsync(cancellationToken), cancellationToken)
            : [];
        return changed.Length > 0
            ? new EntryChangeDto(new Dictionary<string, string>(), changed)
            : Refused("Entries", "None of these entries is served any more.");
    }

    private async Task<EntryChangeDto> CreateWithAsync(
        Entry template,
        Func<Entry, DateTimeOffset, Entry> create,
        CancellationToken cancellationToken)
    {
        var entry = create(template, await serverTime.GetNowAsync(cancellationToken));
        if (await entryRepository.TryAddAsync(entry, cancellationToken))
            return new EntryChangeDto(new Dictionary<string, string>(), [entry.Id]);
        var existing = await entryRepository.GetByKeyHashOrDefaultAsync(template.KeyHash, cancellationToken);
        return existing is null
            ? Refused("Entries", "Another change to this request got there first, try again.")
            : new EntryChangeDto(
                new Dictionary<string, string>
                {
                    ["Entries"] = $"This request already has entry {existing.Id}, change that one instead."
                },
                [existing.Id]);
    }

    private Entry? ReadRequest(EntryRequestDto request, Dictionary<string, string> errors)
    {
        var method = RequestRules.NormalizeMethod(request.Method.Trim());
        if (method is null)
            errors[nameof(request.Method)] = "Must be GET, HEAD, POST, PUT, PATCH or DELETE.";

        var host = request.Host.Trim().ToLowerInvariant();
        if (host.Length == 0)
            errors[nameof(request.Host)] = "Required.";
        else if (host.Length > Entry.HostMaxLength || !HostName().IsMatch(host))
            errors[nameof(request.Host)] = "A host name or address, without a scheme, port or path.";

        var target = request.Target.Trim();
        if (TargetProblem(method, target) is { } targetProblem)
            errors[nameof(request.Target)] = targetProblem;

        var headers = RequestHeaders(request.Headers, out var headersProblem);
        if (headersProblem is not null)
            errors["RequestHeaders"] = headersProblem;

        var body = string.IsNullOrEmpty(request.Body) ? null : request.Body;
        if (body?.Length > Entry.RequestBodyMaxLength)
            errors["RequestBody"] = $"Must be at most {Entry.RequestBodyMaxLength} characters.";

        if (method is null || errors.Count > 0)
            return null;
        var key = EntryKey.Create(method, host, target);
        return new Entry
        {
            KeyHash = key.Hash,
            Method = key.Method,
            Host = key.Host,
            Target = key.Target,
            UserAgent = UserAgentOf(headers),
            RequestHeaders = headers,
            RequestBody = body
        };
    }

    private string? TargetProblem(string? method, string target)
    {
        if (target.Length == 0)
            return "Required.";
        if (target.Any(x => char.IsWhiteSpace(x) || char.IsControl(x)))
            return "Must not contain spaces or control characters, encode them as %20 and the like.";
        if (method is null)
            return null;
        return requestRules.Value.Classify(method, target).Kind switch
        {
            ClassificationKind.BadRequest => "Must start with /.",
            ClassificationKind.UriTooLong => $"Must be at most {requestRules.Value.MaxTargetLength} characters.",
            ClassificationKind.StaticFile => "The trap answers this extension without an entry, so the entry would never be served.",
            _ => null
        };
    }

    private static string? RequestHeaders(string? text, out string? problem)
    {
        problem = null;
        var lines = (text ?? string.Empty)
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToArray();
        foreach (var line in lines)
        {
            var colon = line.IndexOf(':');
            if (colon < 1 || !HeaderName().IsMatch(line[..colon].Trim()))
            {
                problem = $"Write each header as Name: value, \"{line}\" is not.";
                return null;
            }
        }

        var headers = string.Concat(lines.Select(x => x + "\n"));
        if (headers.Length > Entry.RequestHeadersMaxLength)
            problem = $"Must be at most {Entry.RequestHeadersMaxLength} characters.";
        return headers.Length > 0 ? headers : null;
    }

    private static string? UserAgentOf(string? headers)
    {
        var line = headers?
            .Split('\n')
            .FirstOrDefault(x => x.StartsWith("User-Agent:", StringComparison.OrdinalIgnoreCase));
        return line?["User-Agent:".Length..]?.Trim().Truncate(Entry.UserAgentMaxLength);
    }

    private async Task<EntryChangeDto> ReplaceWithAsync(
        IReadOnlyCollection<long> ids,
        Func<Entry, DateTimeOffset, Entry> replace,
        CancellationToken cancellationToken)
    {
        var changeable = ids.Count > 0 ? await entryRepository.GetChangeableAsync(ids, cancellationToken) : [];
        if (changeable.Length == 0)
            return Refused("Entries", "Each of these paths already has a newer entry, change that one instead.");
        var now = await serverTime.GetNowAsync(cancellationToken);
        Entry[] replacements =
        [
            .. changeable
                .GroupBy(x => Convert.ToHexString(x.KeyHash))
                .Select(x => x.OrderBy(entry => entry.Status == EntryStatus.Expired).ThenByDescending(entry => entry.Id).First())
                .Select(x => replace(x, now))
        ];
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
        entry.ResponseHeaders =
        [
            .. headers.Select(x => new ResponseHeader
            {
                Name = x.Name,
                Value = x.Value
            })
        ];
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
            ResponseDelayMilliseconds = current.ResponseDelayMilliseconds,
            Noise = current.Noise,
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
            :
            [
                .. headers, new ResponseHeader
                {
                    Name = "Content-Type",
                    Value = DefaultContentType
                }
            ];
    }

    private static EntryChangeDto Refused(string field, string error)
    {
        return new EntryChangeDto(new Dictionary<string, string>
        {
            [field] = error
        }, []);
    }

    [GeneratedRegex(@"^[!#$%&'*+\-.^_`|~0-9A-Za-z]+$")]
    private static partial Regex HeaderName();

    [GeneratedRegex(@"^(\[[0-9a-f:.]+\]|[a-z0-9]([a-z0-9._-]*[a-z0-9])?)$")]
    private static partial Regex HostName();
}