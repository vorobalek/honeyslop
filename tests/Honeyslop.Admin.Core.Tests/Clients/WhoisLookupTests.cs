using System.Net;
using System.Text;
using System.Text.Json;
using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Clients.Internals;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Honeyslop.Admin.Core.Tests.Clients;

[TestClass]
public sealed class WhoisLookupTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task LookupAsync_NothingStored_AsksRipeStatAndStoresTheAnswer()
    {
        var handler = new RipeStatHandler();
        var repository = new StoredWhois();

        var info = await Lookup(handler, repository).LookupAsync("8.8.8.8", CancellationToken.None);

        Assert.AreEqual("8.8.8.0/24", info.Prefix);
        Assert.HasCount(3, handler.Requests);
        Assert.IsTrue(handler.Requests.All(x => x.Query.Contains("resource=8.8.8.8", StringComparison.Ordinal)));
        Assert.IsTrue(handler.UserAgents.All(x => x == FixedUserAgent.Value));
        Assert.AreEqual(Now, repository.Rows["8.8.8.8"].UpdatedAt);
    }

    [TestMethod]
    public async Task LookupAsync_StoredWithinTwelveHours_ComesFromTheTable()
    {
        var handler = new RipeStatHandler();
        var repository = new StoredWhois();
        repository.Store("8.8.8.8", Stored("stored/24"), Now.AddHours(-11));

        var info = await Lookup(handler, repository).LookupAsync("8.8.8.8", CancellationToken.None);

        Assert.AreEqual("stored/24", info.Prefix);
        Assert.IsEmpty(handler.Requests);
    }

    [TestMethod]
    public async Task LookupAsync_StoredLongerThanTwelveHoursAgo_IsRefreshed()
    {
        var handler = new RipeStatHandler();
        var repository = new StoredWhois();
        repository.Store("8.8.8.8", Stored("stored/24"), Now.AddHours(-13));

        var info = await Lookup(handler, repository).LookupAsync("8.8.8.8", CancellationToken.None);

        Assert.AreEqual("8.8.8.0/24", info.Prefix);
        Assert.AreEqual(Now, repository.Rows["8.8.8.8"].UpdatedAt);
    }

    [TestMethod]
    public async Task LookupAsync_RipeStatFailsOnAStaleAnswer_ShowsTheStaleOne()
    {
        var handler = new RipeStatHandler { Status = HttpStatusCode.ServiceUnavailable };
        var repository = new StoredWhois();
        repository.Store("8.8.8.8", Stored("stored/24"), Now.AddDays(-3));

        var info = await Lookup(handler, repository).LookupAsync("8.8.8.8", CancellationToken.None);

        Assert.AreEqual("stored/24", info.Prefix);
    }

    [TestMethod]
    public async Task LookupAsync_RipeStatFailsWithNothingStored_Throws()
    {
        var handler = new RipeStatHandler { Status = HttpStatusCode.ServiceUnavailable };

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            Lookup(handler, new StoredWhois()).LookupAsync("8.8.8.8", CancellationToken.None));
    }

    private static WhoisLookup Lookup(HttpMessageHandler handler, IClientWhoisRepository repository)
    {
        var httpClient = new HttpClient(handler);
        RipeStatClient.Configure(httpClient);
        var client = new RipeStatClient(httpClient, new FixedUserAgent());
        return new WhoisLookup(client, repository, new FixedTime(), NullLogger<WhoisLookup>.Instance);
    }

    private static WhoisDto Stored(string prefix)
    {
        return new WhoisDto(prefix, [], null, null, null, null, null, [], string.Empty, Now);
    }

    private sealed class FixedUserAgent : IUserAgentProvider
    {
        public const string Value = "Honeyslop-test (+https://example.test)";

        public Task<string> GetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Value);
        }
    }

    private sealed class FixedTime : IServerTimeRepository
    {
        public Task<DateTimeOffset> GetNowAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Now);
        }
    }

    private sealed class StoredWhois : IClientWhoisRepository
    {
        public Dictionary<string, ClientWhois> Rows { get; } = [];

        public void Store(string clientIp, WhoisDto info, DateTimeOffset updatedAt)
        {
            Rows[clientIp] = new ClientWhois { ClientIp = clientIp, Info = JsonSerializer.Serialize(info), UpdatedAt = updatedAt };
        }

        public Task<ClientWhois?> GetOrDefaultAsync(string clientIp, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.GetValueOrDefault(clientIp));
        }

        public Task SaveAsync(string clientIp, string info, DateTimeOffset now, CancellationToken cancellationToken)
        {
            Rows[clientIp] = new ClientWhois { ClientIp = clientIp, Info = info, UpdatedAt = now };
            return Task.CompletedTask;
        }
    }

    private sealed class RipeStatHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        public List<Uri> Requests { get; } = [];
        public List<string> UserAgents { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (Requests)
            {
                Requests.Add(request.RequestUri!);
                UserAgents.Add(request.Headers.UserAgent.ToString());
            }
            var body = request.RequestUri!.AbsolutePath switch
            {
                "/data/prefix-overview/data.json" => RipeStatFixtures.PrefixOverview,
                "/data/abuse-contact-finder/data.json" => RipeStatFixtures.AbuseContactFinder,
                "/data/whois/data.json" => RipeStatFixtures.Whois,
                _ => "{}"
            };
            return Task.FromResult(new HttpResponseMessage(Status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
