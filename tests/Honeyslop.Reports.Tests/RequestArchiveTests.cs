using System.IO.Compression;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Moq;

namespace Honeyslop.Reports.Tests;

[TestClass]
public sealed class RequestArchiveTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Add_KeepsWhatTheClientSentAndDropsOurProxyHeaders()
    {
        using var archive = new RequestArchive("203.0.113.9", 5, 2);
        archive.Add(Request(1, Now.AddMinutes(-1), "GET", "/.env", "Host: a.example.test\nX-Forwarded-Server: traefik-1\n", null));
        archive.Add(Request(2, Now, "POST", "/wp-login.php", "Host: a.example.test\nX-Forwarded-For: 10.0.0.1\nX-Real-Ip: 203.0.113.9\nX-Original-Proto: http\nUser-Agent: curl/8.4\n", "log=admin&pwd=x"));

        var text = Unzip(archive.ToArray());

        Assert.AreEqual(2, archive.Count);
        Assert.StartsWith("The latest 2 of the 5 requests we kept from 203.0.113.9, oldest first", text);
        Assert.IsLessThan(text.IndexOf("POST /wp-login.php", StringComparison.Ordinal), text.IndexOf("GET /.env", StringComparison.Ordinal));
        Assert.Contains("=== 2026-11-18 12:00:00 UTC ===\nPOST /wp-login.php\nHost: a.example.test\nUser-Agent: curl/8.4\n\nlog=admin&pwd=x\n", text);
        Assert.DoesNotContain("X-Forwarded", text);
        Assert.DoesNotContain("X-Real-Ip", text);
        Assert.DoesNotContain("X-Original", text);
    }

    [TestMethod]
    public async Task BuildAsync_TakesTheLatestRequestsInPagesAndRemembersTheLastOne()
    {
        RequestRecord[] requests = [.. Enumerable.Range(1, 2500).Select(x => Request(x, Now.AddSeconds(x), "GET", $"/{x}", "Host: a.example.test\n", null))];
        var repository = Repository(requests);

        var archive = await new RequestArchiveBuilder(repository.Object).BuildAsync("203.0.113.9", 2200, CancellationToken.None);

        Assert.IsNotNull(archive);
        Assert.AreEqual(2200, archive.Requests);
        Assert.AreEqual(2500, archive.Total);
        Assert.AreEqual(2500, archive.LastRequestId);
        var text = Unzip(archive.Content);
        Assert.StartsWith("The latest 2200 of the 2500 requests", text);
        Assert.DoesNotContain("GET /300\n", text);
        Assert.Contains("GET /301\n", text);
        Assert.Contains("GET /2500\n", text);
        repository.Verify(x => x.GetLogAsync("203.0.113.9", It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [TestMethod]
    public async Task BuildAsync_NoRequests_GivesNothing()
    {
        var archive = await new RequestArchiveBuilder(Repository([]).Object).BuildAsync("203.0.113.9", 100, CancellationToken.None);

        Assert.IsNull(archive);
    }

    private static Mock<IRequestRepository> Repository(RequestRecord[] requests)
    {
        var repository = new Mock<IRequestRepository>();
        repository
            .Setup(x => x.CountAsync(It.IsAny<RequestFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(requests.Length);
        repository
            .Setup(x => x.GetLogStartAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, int latest, CancellationToken _) =>
                requests.OrderByDescending(x => x.Id).Take(latest).Select(x => (long?)x.Id).LastOrDefault());
        repository
            .Setup(x => x.GetLogAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, long fromId, int limit, CancellationToken _) =>
                [.. requests.Where(x => x.Id >= fromId).OrderBy(x => x.Id).Take(limit)]);
        return repository;
    }

    private static RequestRecord Request(long id, DateTimeOffset receivedAt, string method, string target, string headers, string? body)
    {
        return new RequestRecord
        {
            Id = id,
            ReceivedAt = receivedAt,
            ClientIp = "203.0.113.9",
            Method = method,
            Host = "a.example.test",
            Target = target,
            Headers = headers,
            Body = body,
            Outcome = RequestOutcome.Hit,
            Instance = "test"
        };
    }

    private static string Unzip(byte[] archive)
    {
        using var zip = new ZipArchive(new MemoryStream(archive), ZipArchiveMode.Read);
        using var reader = new StreamReader(zip.GetEntry(RequestArchive.FileName)!.Open());
        return reader.ReadToEnd();
    }
}
