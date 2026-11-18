using System.Net;
using MaxMind.Db;

namespace Honeyslop.Admin.Clients;

internal sealed partial class CountryLookup : ICountryLookup, IDisposable
{
    private readonly Reader? reader;

    public CountryLookup(string path, ILogger<CountryLookup> logger)
    {
        if (!File.Exists(path))
        {
            LogMissing(logger, path);
            return;
        }

        reader = new Reader(path);
        LogLoaded(logger, reader.Metadata.DatabaseType, reader.Metadata.BuildDate, path);
    }

    public Country? Find(string clientIp)
    {
        if (reader is null || !IPAddress.TryParse(clientIp, out var address))
            return null;

        var record = reader.Find<Dictionary<string, object>>(address);
        if (record?.GetValueOrDefault("country") is not Dictionary<string, object> country ||
            country.GetValueOrDefault("iso_code") is not string { Length: 2 } code ||
            !code.All(char.IsAsciiLetter))
            return null;

        var names = country.GetValueOrDefault("names") as Dictionary<string, object>;
        var upperCode = code.ToUpperInvariant();
        return new Country(upperCode, names?.GetValueOrDefault("en") as string ?? upperCode);
    }

    public void Dispose()
    {
        reader?.Dispose();
    }

    [LoggerMessage(LogLevel.Warning, "No country database at {path}: client addresses are shown without flags")]
    static partial void LogMissing(ILogger<CountryLookup> logger, string path);

    [LoggerMessage(LogLevel.Information, "Country database {databaseType} built {buildDate} loaded from {path}")]
    static partial void LogLoaded(ILogger<CountryLookup> logger, string databaseType, DateTime buildDate, string path);
}
