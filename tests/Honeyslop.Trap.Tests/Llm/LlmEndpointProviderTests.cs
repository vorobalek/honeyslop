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
            endpoints.Select(x => x.Authority).ToArray());
    }
}
