using System.Security.Cryptography;

namespace Honeyslop.Tests;

public static class Unique
{
    public static int Int32()
    {
        return BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4));
    }

    public static long Int64()
    {
        return BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8));
    }

    public static string String(int? length = null)
    {
        return RandomNumberGenerator.GetString(
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_", length ?? 32);
    }

    public static string Host()
    {
        return $"{String(12).ToLowerInvariant()}.example.test";
    }

    public static string Target()
    {
        return $"/{String(16)}";
    }
}