using System.Net;
using System.Net.Sockets;

namespace Honeyslop.Data.Requests;

public static class ClientAddress
{
    public static string Key(IPAddress address)
    {
        return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }

    public static string[] Spellings(string key)
    {
        return IPAddress.TryParse(key, out var address) && address.AddressFamily == AddressFamily.InterNetwork
            ? [key, address.MapToIPv6().ToString()]
            : [key];
    }
}