using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Core.Clients;

public interface ICountryLookup
{
    CountryDto? Find(string clientIp);
    IReadOnlyDictionary<string, CountryDto> FindAll(IEnumerable<string> clientIps);
}