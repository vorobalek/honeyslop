namespace Honeyslop.Admin.Core.Options;

public sealed class AbuseReportOptions
{
    private static readonly char[] Specials = ['(', ')', '<', '>', '[', ']', ':', ';', '@', '\\', ',', '.', '"'];

    public string? DefaultSenderName { get; set; }
    public string? FromAddress { get; set; }

    public string? ExpectedFrom(string? senderName)
    {
        if (string.IsNullOrWhiteSpace(FromAddress))
            return null;
        if (string.IsNullOrWhiteSpace(senderName))
            return FromAddress;
        var name = senderName.IndexOfAny(Specials) < 0
            ? senderName
            : $"\"{senderName.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        return $"{name} <{FromAddress}>";
    }
}