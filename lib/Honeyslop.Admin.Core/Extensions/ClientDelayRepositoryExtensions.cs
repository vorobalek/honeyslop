using System.Net;
using Honeyslop.Data.Repositories;
using Honeyslop.Data.Requests;

namespace Honeyslop.Admin.Core.Extensions;

internal static class ClientDelayRepositoryExtensions
{
    public static string Key(string clientIp)
    {
        return IPAddress.TryParse(clientIp, out var address) ? ClientAddress.Key(address) : clientIp;
    }

    private static (string Ip, string Key)[] Keys(IEnumerable<string> clientIps)
    {
        return [.. clientIps.Distinct(StringComparer.Ordinal).Select(x => (x, Key(x)))];
    }

    private static Dictionary<string, int> ByIp((string Ip, string Key)[] keys, Dictionary<string, int> delays)
    {
        return keys
            .Where(x => delays.ContainsKey(x.Key))
            .ToDictionary(x => x.Ip, x => delays[x.Key], StringComparer.Ordinal);
    }

    extension(IClientDelayRepository repository)
    {
        public async Task<IReadOnlyDictionary<string, int>> FindAllAsync(IEnumerable<string> clientIps, CancellationToken cancellationToken)
        {
            var keys = Keys(clientIps);
            if (keys.Length == 0)
                return new Dictionary<string, int>();
            var delays = await repository.GetAnyOfAsync([.. keys.Select(x => x.Key).Distinct(StringComparer.Ordinal)], cancellationToken);
            return ByIp(keys, delays.ToDictionary(x => x.ClientIp, x => x.ResponseDelayMilliseconds, StringComparer.Ordinal));
        }
    }

    extension(IClientProbeRepository repository)
    {
        public async Task<IReadOnlyDictionary<string, int>> FindAllAsync(IEnumerable<string> clientIps, CancellationToken cancellationToken)
        {
            var keys = Keys(clientIps);
            if (keys.Length == 0)
                return new Dictionary<string, int>();
            var probes = await repository.GetAnyOfAsync([.. keys.Select(x => x.Key).Distinct(StringComparer.Ordinal)], cancellationToken);
            return ByIp(keys, probes.ToDictionary(x => x.ClientIp, x => x.DelayMilliseconds, StringComparer.Ordinal));
        }
    }
}