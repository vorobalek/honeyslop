using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Pages;

public sealed record ClientIpLink(string Ip, CountryDto? Country)
{
    public static ClientIpLink From(string ip, IReadOnlyDictionary<string, CountryDto> countries)
    {
        return new ClientIpLink(ip, countries.GetValueOrDefault(ip));
    }
}
