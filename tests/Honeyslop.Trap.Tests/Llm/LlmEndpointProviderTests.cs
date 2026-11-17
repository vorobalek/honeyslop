using Honeyslop.Llm;
using Honeyslop.Trap.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Tests.Llm;

[TestClass]
public sealed class LlmEndpointProviderTests
{
    [TestMethod]
    public async Task GetEndpointsAsync_SeveralUrls_ReturnsEveryServer()
    {
        var provider = new LlmEndpointProvider(
            new OptionsWrapper<LlmOptions>(new LlmOptions
            {
                Urls = [new Uri("http://10.0.0.2:8080/"), new Uri("http://10.0.0.1:8080/"), new Uri("http://10.0.0.1:8081/")],
                DiscoveryInterval = TimeSpan.FromMinutes(1)
            }),
            TimeProvider.System,
            NullLogger<LlmEndpointProvider>.Instance);

        var endpoints = await provider.GetEndpointsAsync(CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { "10.0.0.1:8080", "10.0.0.1:8081", "10.0.0.2:8080" },
            endpoints.Select(x => x.Url.Authority).ToArray());
        CollectionAssert.AreEqual(
            endpoints.Select(x => x.Url.Authority).ToArray(),
            endpoints.Select(x => x.Name).ToArray());
    }

    [TestMethod]
    public async Task GetEndpointsAsync_HostName_NamesTheServerByReverseLookup()
    {
        var provider = new LlmEndpointProvider(
            new OptionsWrapper<LlmOptions>(new LlmOptions
            {
                Urls = [new Uri("http://localhost:8080/")],
                DiscoveryInterval = TimeSpan.FromMinutes(1)
            }),
            TimeProvider.System,
            NullLogger<LlmEndpointProvider>.Instance);

        var endpoint = (await provider.GetEndpointsAsync(CancellationToken.None)).Single();

        Assert.AreEqual("127.0.0.1:8080", endpoint.Url.Authority);
        Assert.AreEqual("localhost", endpoint.Name);
    }
}
