using System.Security.Cryptography;
using System.Text;

namespace Honeyslop.Trap.Requests;

public sealed record EntryKey(string Method,
    string Host,
    string Target,
    byte[] Hash)
{
    public string HashHex => Convert.ToHexString(Hash);

    public static EntryKey Create(string method, string host, string target)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{method}\n{host}\n{target}"));
        return new EntryKey(method, host, target, hash);
    }
}