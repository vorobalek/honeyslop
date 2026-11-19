using System.Text.Json;
using System.Text.RegularExpressions;
using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Core.Clients.Internals;

internal static partial class WhoisParser
{
    private static readonly string[] RangeKeys = ["CIDR", "inetnum", "inet6num", "NetRange"];
    private static readonly string[] NameKeys = ["NetName"];
    private static readonly string[] OwnerKeys = ["Organization", "OrgName", "org-name", "owner", "descr"];
    private static readonly string[] CountryKeys = ["country"];

    private static readonly Dictionary<string, string> Registries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["afrinic"] = "AFRINIC",
        ["apnic"] = "APNIC",
        ["arin"] = "ARIN",
        ["lacnic"] = "LACNIC",
        ["ripe"] = "RIPE NCC"
    };

    public static WhoisDto Parse(
        JsonElement prefixOverview,
        JsonElement abuseContactFinder,
        JsonElement whois,
        DateTimeOffset lookedUpAt)
    {
        Field[][] records = [.. Items(whois, "records").Select(ReadRecord).Where(x => x.Length > 0)];
        var network = records.LastOrDefault(x => Find(x, RangeKeys) is not null) ?? [];
        var announced = prefixOverview.ValueKind == JsonValueKind.Object &&
                        prefixOverview.TryGetProperty("announced", out var flag) &&
                        flag.ValueKind == JsonValueKind.True;
        var registry = Text(abuseContactFinder, "authoritative_rir") ??
                       Items(whois, "authorities").Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : null).FirstOrDefault();

        return new WhoisDto(
            announced ? Text(prefixOverview, "resource") : null,
            announced ? [.. Items(prefixOverview, "asns").Select(ReadOrigin).OfType<WhoisOriginDto>()] : [],
            Find(network, RangeKeys),
            Find(network, NameKeys),
            Find(network, OwnerKeys) ?? records.Select(x => Find(x, OwnerKeys)).FirstOrDefault(x => x is not null),
            Find(network, CountryKeys) ?? records.Select(x => Find(x, CountryKeys)).FirstOrDefault(x => x is not null),
            registry is null ? null : Registries.GetValueOrDefault(registry, registry.ToUpperInvariant()),
            [
                .. Items(abuseContactFinder, "abuse_contacts")
                    .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString()?.Trim() : null)
                    .OfType<string>()
                    .Where(x => EmailPattern().IsMatch(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            ],
            string.Join("\n\n", records.Select(record => string.Join("\n", record.Select(x => $"{x.Key}: {x.Value}")))),
            lookedUpAt);
    }

    private static IEnumerable<JsonElement> Items(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(name, out var items) &&
               items.ValueKind == JsonValueKind.Array
            ? items.EnumerateArray()
            : [];
    }

    private static string? Text(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(name, out var value) &&
               value.ValueKind == JsonValueKind.String &&
               !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;
    }

    private static Field[] ReadRecord(JsonElement record)
    {
        return record.ValueKind == JsonValueKind.Array
            ? [.. record.EnumerateArray().Select(x => new Field(Text(x, "key") ?? string.Empty, Text(x, "value") ?? string.Empty)).Where(x => x.Key.Length > 0)]
            : [];
    }

    private static WhoisOriginDto? ReadOrigin(JsonElement origin)
    {
        return origin.ValueKind == JsonValueKind.Object &&
               origin.TryGetProperty("asn", out var asn) &&
               asn.TryGetInt64(out var number)
            ? new WhoisOriginDto(number, Text(origin, "holder") ?? string.Empty)
            : null;
    }

    private static string? Find(Field[] record, string[] keys)
    {
        return keys
            .Select(key => record.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && x.Value.Length > 0)?.Value)
            .FirstOrDefault(x => x is not null);
    }

    [GeneratedRegex(@"^[^@\s?&]+@[^@\s?&]+\.[^@\s?&]+$")]
    private static partial Regex EmailPattern();

    private sealed record Field(string Key,
        string Value);
}