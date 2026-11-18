using System.Globalization;
using System.IO.Compression;
using System.Text;
using Honeyslop.Data.Entities;

namespace Honeyslop.Reports;

public sealed class RequestArchive : IDisposable
{
    public const string FileName = "requests.txt";

    private static readonly string[] ProxyHeaderPrefixes = ["X-Forwarded-", "X-Original-", "X-Real-Ip:"];

    private readonly MemoryStream _stream = new();
    private readonly ZipArchive _zip;
    private readonly StreamWriter _writer;

    public RequestArchive(string clientIp, int total, int included)
    {
        _zip = new ZipArchive(_stream, ZipArchiveMode.Create, true);
        _writer = new StreamWriter(_zip.CreateEntry(FileName, CompressionLevel.Optimal).Open(), new UTF8Encoding(false));
        var scope = included < total
            ? string.Create(CultureInfo.InvariantCulture, $"The latest {included} of the {total} requests")
            : string.Create(CultureInfo.InvariantCulture, $"All {total} requests");
        _writer.Write($"{scope} we kept from {clientIp}, oldest first, as our servers received them.\n");
        _writer.Write("Times are UTC. Cookie values are left out, long bodies were cut when we recorded them, ");
        _writer.Write("and the headers our own proxy adds are removed.\n");
    }

    public int Count { get; private set; }

    public void Add(RequestRecord request)
    {
        _writer.Write("\n=== ");
        _writer.Write(request.ReceivedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        _writer.Write(" UTC ===\n");
        _writer.Write(request.Method);
        _writer.Write(' ');
        _writer.Write(request.Target);
        _writer.Write('\n');
        foreach (var header in (request.Headers ?? string.Empty).Split('\n'))
            if (header.Length > 0 && !IsProxyHeader(header))
            {
                _writer.Write(header);
                _writer.Write('\n');
            }

        if (!string.IsNullOrEmpty(request.Body))
        {
            _writer.Write('\n');
            _writer.Write(request.Body);
            _writer.Write('\n');
        }

        Count++;
    }

    public byte[] ToArray()
    {
        _writer.Dispose();
        _zip.Dispose();
        return _stream.ToArray();
    }

    public void Dispose()
    {
        _writer.Dispose();
        _zip.Dispose();
        _stream.Dispose();
    }

    private static bool IsProxyHeader(string header)
    {
        return ProxyHeaderPrefixes.Any(x => header.StartsWith(x, StringComparison.OrdinalIgnoreCase));
    }
}
