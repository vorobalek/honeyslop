namespace Honeyslop.Admin.Core.Text;

public static class UserAgentHeader
{
    public const int MaxLength = 512;

    public static bool IsValid(string value)
    {
        if (value.Length > MaxLength || value.Any(char.IsControl))
            return false;
        using var request = new HttpRequestMessage();
        return request.Headers.UserAgent.TryParseAdd(value);
    }
}
