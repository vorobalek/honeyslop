using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Text;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;

namespace Honeyslop.Admin.Core.Tests.Clients;

[TestClass]
public sealed class AbuseLetterTests
{
    private static readonly DateTimeOffset First = new(2026, 11, 17, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Last = new(2026, 11, 18, 9, 30, 15, TimeSpan.Zero);

    [TestMethod]
    public void Create_ListsTheNetworkTheTotalsAndTheLatestRequests()
    {
        var client = Client(42, 3, 17, First, Last, "curl/8.4");
        var whois = new WhoisDto("203.0.113.0/24", [new WhoisOriginDto(64500, "EXAMPLE-AS")], null, null, null, null, "RIPE NCC", [], string.Empty, Last);
        RequestRecord[] latest =
        [
            new() { Id = 2, ReceivedAt = Last, ClientIp = client.ClientIp, Method = "GET", Host = "a.example.test", Target = "/.env", UserAgent = "curl/8.4" },
            new() { Id = 1, ReceivedAt = First, ClientIp = client.ClientIp, Method = "POST", Host = "b.example.test", Target = "/wp-login.php" }
        ];

        var report = AbuseLetters.Write(client, latest, whois);

        Assert.AreEqual("HTTP scanning from 203.0.113.9", report.Subject);
        Assert.Contains("203.0.113.9 (203.0.113.0/24, AS64500 EXAMPLE-AS) sent 42 HTTP requests for 17 paths to 3 host names we operate", report.Body);
        Assert.Contains("between 2026-11-17 08:00:00 and 2026-11-18 09:30:15 UTC", report.Body);
        Assert.Contains("The latest 2 requests (UTC)", report.Body);
        Assert.Contains("2026-11-18 09:30:15  GET a.example.test/.env  \"curl/8.4\"\n", report.Body);
        Assert.Contains("2026-11-17 08:00:00  POST b.example.test/wp-login.php\n", report.Body);
    }

    [TestMethod]
    public void Create_WithoutWhois_LeavesTheNetworkOut()
    {
        var client = Client(1, 1, 1, First, First, null);

        var report = AbuseLetters.Write(client, [], null);

        Assert.Contains("203.0.113.9 sent 1 HTTP request for 1 path to 1 host name we operate", report.Body);
        Assert.Contains("The latest 0 requests (UTC)", report.Body);
        Assert.DoesNotContain("Regards", report.Body);
    }

    private static ClientActivity Client(int requests, int hosts, int targets, DateTimeOffset first, DateTimeOffset last, string? userAgent)
    {
        return new ClientActivity
        {
            ClientIp = "203.0.113.9",
            Requests = requests,
            Hosts = hosts,
            Targets = targets,
            FirstSeenAt = first,
            LastSeenAt = last,
            LastUserAgent = userAgent
        };
    }
}
