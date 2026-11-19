using System.Text.RegularExpressions;

namespace Honeyslop.Reports;

public static partial class AbuseContactBlocklist
{
    public static string[] Parse(IEnumerable<string> contacts)
    {
        return
        [
            .. contacts
                .SelectMany(x => x.Split(['\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(x => x.ToLowerInvariant())
                .Distinct(StringComparer.Ordinal)
        ];
    }

    public static string? Problem(IEnumerable<string> entries)
    {
        return entries.FirstOrDefault(x => !Entry().IsMatch(x)) is { } wrong
            ? $"\"{wrong}\" is neither an address nor an @domain."
            : null;
    }

    public static string[] Candidates(IEnumerable<string> addresses)
    {
        return
        [
            .. addresses
                .Select(x => x.Trim().ToLowerInvariant())
                .SelectMany(x => x.IndexOf('@') is var at and > 0
                    ? [x, x[at..]]
                    : new[]
                    {
                        x
                    })
                .Distinct(StringComparer.Ordinal)
        ];
    }

    public static bool Blocks(IReadOnlyCollection<string> blocked, string address)
    {
        var normalized = address.Trim().ToLowerInvariant();
        return blocked.Any(x => x.StartsWith('@') ? normalized.EndsWith(x, StringComparison.Ordinal) : normalized == x);
    }

    [GeneratedRegex(@"^([^@\s]+)?@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Entry();
}