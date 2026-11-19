using System.Net;

namespace Honeyslop.Data.Requests;

public static class ClientAddress
{
    public static string Key(IPAddress address)
    {
        return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }
}