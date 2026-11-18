using System.Net;
using System.Text;
using Honeyslop.Admin.Clients;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Honeyslop.Admin.Tests.Clients;

[TestClass]
public sealed class WhoisLookupTests
{
    [TestMethod]
    public async Task LookupAsync_SecondTime_ComesFromTheCache()
    {
        var handler = new RipeStatHandler();
        var cache = new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));

        var first = await Lookup(handler, cache).LookupAsync("8.8.8.8", CancellationToken.None);
        var second = await Lookup(handler, cache).LookupAsync("8.8.8.8", CancellationToken.None);

        Assert.AreEqual(3, handler.Requests.Count);
        Assert.AreEqual(first.Network, second.Network);
        CollectionAssert.AreEqual(first.Origins, second.Origins);
        CollectionAssert.AreEqual(first.AbuseContacts, second.AbuseContacts);
        Assert.IsTrue(handler.Requests.All(x => x.Query.Contains("resource=8.8.8.8", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task LookupAsync_BrokenCache_StillAnswers()
    {
        var handler = new RipeStatHandler();

        var info = await Lookup(handler, new BrokenCache()).LookupAsync("8.8.8.8", CancellationToken.None);

        Assert.AreEqual("8.8.8.0/24", info.Prefix);
    }

    [TestMethod]
    public async Task LookupAsync_RipeStatFails_Throws()
    {
        var handler = new RipeStatHandler { Status = HttpStatusCode.ServiceUnavailable };

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            Lookup(handler, new NoDistributedCache()).LookupAsync("8.8.8.8", CancellationToken.None));
    }

    private static WhoisLookup Lookup(HttpMessageHandler handler, IDistributedCache cache)
    {
        var client = new RipeStatClient(new HttpClient(handler) { BaseAddress = RipeStatClient.BaseAddress });
        return new WhoisLookup(client, cache, TimeProvider.System, NullLogger<WhoisLookup>.Instance);
    }

    private sealed class RipeStatHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (Requests)
                Requests.Add(request.RequestUri!);
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

    private sealed class BrokenCache : IDistributedCache
    {
        public byte[] Get(string key)
        {
            throw new InvalidOperationException("Redis is down");
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            throw new InvalidOperationException("Redis is down");
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            throw new InvalidOperationException("Redis is down");
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            throw new InvalidOperationException("Redis is down");
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }
    }
}
