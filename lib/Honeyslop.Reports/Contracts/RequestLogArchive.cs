namespace Honeyslop.Reports.Contracts;

public sealed record RequestLogArchive(byte[] Content,
    int Requests,
    int Total,
    long LastRequestId)
{
    public static string FileName(string clientIp)
    {
        return $"{clientIp.Replace(':', '-')}-requests.zip";
    }
}