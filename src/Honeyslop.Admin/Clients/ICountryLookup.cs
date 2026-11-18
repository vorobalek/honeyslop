namespace Honeyslop.Admin.Clients;

public interface ICountryLookup
{
    Country? Find(string clientIp);
}
