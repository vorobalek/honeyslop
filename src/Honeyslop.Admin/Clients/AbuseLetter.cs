using System.Globalization;
using System.Text;
using Honeyslop.Admin.Extensions;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Clients;

public sealed record AbuseLetter(string Subject, string Body)
{
    public const int LatestRequests = 20;
    public const int MaxAttachedRequests = 50000;

    public static AbuseLetter Create(
        ClientSummary client,
        IReadOnlyCollection<RequestSummary> latest,
        WhoisInfo? whois)
    {
        var network = whois is null
            ? string.Empty
            : string.Join(", ", new[] { whois.Prefix ?? whois.Network }
                .Concat(whois.Origins.Select(x => $"AS{x.Asn} {x.Holder}".TrimEnd()))
                .Where(x => !string.IsNullOrEmpty(x)));
        var builder = new StringBuilder()
            .Append("Hello,\n\n")
            .Append(client.ClientIp);
        if (network.Length > 0)
            builder.Append(" (").Append(network).Append(')');
        builder
            .Append(CultureInfo.InvariantCulture, $" sent {client.Requests.Of("HTTP request")} for {client.Targets.Of("path")} ")
            .Append(CultureInfo.InvariantCulture, $"to {client.Hosts.Of("host name")} we operate ")
            .Append(CultureInfo.InvariantCulture, $"between {client.FirstSeenAt.Display} and {client.LastSeenAt.Display} UTC. ")
            .Append("None of these paths exist on our servers; the pattern matches automated vulnerability scanning.\n\n")
            .Append(CultureInfo.InvariantCulture, $"The latest {latest.Count.Of("request")} (UTC):\n\n");
        foreach (var request in latest)
        {
            builder
                .Append(request.ReceivedAt.Display)
                .Append("  ")
                .Append(request.Method)
                .Append(' ')
                .Append(request.Host)
                .Append(request.Target);
            if (!string.IsNullOrEmpty(request.UserAgent))
                builder.Append("  \"").Append(request.UserAgent).Append('"');
            builder.Append('\n');
        }

        builder
            .Append("\nThe attached archive holds every request we kept from this address, ")
            .Append("with its headers and body as we received them.\n")
            .Append("\nPlease look into this and stop the traffic from this address.\n");
        return new AbuseLetter($"HTTP scanning from {client.ClientIp}", builder.ToString());
    }
}
