using System.Net;
using Honeyslop.Admin.Core.Contracts;
using MaxMind.Db;

namespace Honeyslop.Admin.Core.Clients.Internals;

internal sealed partial class CountryLookup : ICountryLookup, IDisposable
{
    private readonly Reader? _reader;

    public CountryLookup(string path, ILogger<CountryLookup> logger)
    {
        if (!File.Exists(path))
        {
            LogMissing(logger, path);
            return;
        }

        _reader = new Reader(path);
        LogLoaded(logger, _reader.Metadata.DatabaseType, _reader.Metadata.BuildDate, path);
    }

    public CountryDto? Find(string clientIp)
    {
        if (_reader is null || !IPAddress.TryParse(clientIp, out var address))
            return null;

        var record = _reader.Find<Dictionary<string, object>>(address);
        if (record?.GetValueOrDefault("country") is not Dictionary<string, object> country ||
            country.GetValueOrDefault("iso_code") is not string { Length: 2 } code ||
            !code.All(char.IsAsciiLetter))
            return null;

        var names = country.GetValueOrDefault("names") as Dictionary<string, object>;
        var upperCode = code.ToUpperInvariant();
        return new CountryDto(upperCode, names?.GetValueOrDefault("en") as string ?? upperCode);
    }

    public IReadOnlyDictionary<string, CountryDto> FindAll(IEnumerable<string> clientIps)
    {
        var countries = new Dictionary<string, CountryDto>(StringComparer.Ordinal);
        foreach (var clientIp in clientIps)
            if (!countries.ContainsKey(clientIp) && Find(clientIp) is { } country)
                countries[clientIp] = country;
        return countries;
    }

    public void Dispose()
    {
        _reader?.Dispose();
    }

    [LoggerMessage(LogLevel.Warning, "No country database at {path}: client addresses are shown without flags")]
    static partial void LogMissing(ILogger<CountryLookup> logger, string path);

    [LoggerMessage(LogLevel.Information, "Country database {databaseType} built {buildDate} loaded from {path}")]
    static partial void LogLoaded(ILogger<CountryLookup> logger, string databaseType, DateTime buildDate, string path);
}
