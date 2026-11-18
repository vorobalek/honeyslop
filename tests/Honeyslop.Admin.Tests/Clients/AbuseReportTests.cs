using Honeyslop.Admin.Clients;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Tests.Clients;

[TestClass]
public sealed class AbuseReportTests
{
    private static readonly DateTimeOffset First = new(2026, 11, 17, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Last = new(2026, 11, 18, 9, 30, 15, TimeSpan.Zero);

    [TestMethod]
    public void Create_ListsTheNetworkTheTotalsAndTheLatestRequests()
    {
        var client = new ClientSummary("203.0.113.9", 42, 3, 17, First, Last, "curl/8.4");
        var whois = new WhoisInfo("203.0.113.0/24", [new WhoisOrigin(64500, "EXAMPLE-AS")], null, null, null, null, "RIPE NCC", [], string.Empty, Last);
        RequestSummary[] latest =
        [
            new(2, Last, client.ClientIp, "GET", "a.example.test", "/.env", "curl/8.4", RequestOutcome.Hit, 200, 1, 3),
            new(1, First, client.ClientIp, "POST", "b.example.test", "/wp-login.php", null, RequestOutcome.Generated, 403, 2, 900)
        ];

        var report = AbuseReport.Create(client, latest, whois);

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
        var client = new ClientSummary("203.0.113.9", 1, 1, 1, First, First, null);

        var report = AbuseReport.Create(client, [], null);

        Assert.Contains("203.0.113.9 sent 1 HTTP request for 1 path to 1 host name we operate", report.Body);
        Assert.Contains("The latest 0 requests (UTC)", report.Body);
    }
}
