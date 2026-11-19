using System.Text.RegularExpressions;
using Honeyslop.Data.Entities;

namespace Honeyslop.Admin.Core.Text;

public static partial class SenderName
{
    public const int MaxLength = AbuseReport.SenderNameMaxLength;

    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var cleaned = Whitespace().Replace(ControlCharacters().Replace(value, " "), " ").Trim();
        if (cleaned.Length > AbuseReport.SenderNameMaxLength)
            cleaned = cleaned[..AbuseReport.SenderNameMaxLength].TrimEnd();
        return cleaned.Length > 0 ? cleaned : null;
    }

    [GeneratedRegex(@"\p{C}")]
    private static partial Regex ControlCharacters();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}