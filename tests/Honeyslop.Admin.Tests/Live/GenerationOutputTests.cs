using System.Text;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Tests.Live;

[TestClass]
public sealed class GenerationOutputTests
{
    [TestMethod]
    public void Payload_RoundTrips()
    {
        var output = new GenerationOutput(42, 3, "{\"status\":200,\"body\":\"<h1>Привет</h1>\"}");

        Assert.AreEqual(output, GenerationOutput.FromPayload(output.ToPayload()));
    }

    [TestMethod]
    public void Payload_LongOutput_KeepsTheTailWithinTheNotifyLimit()
    {
        var content = string.Concat(Enumerable.Range(0, 2000).Select(x => $"строка {x}\n"));
        var output = new GenerationOutput(7, 1024, content);

        var payload = output.ToPayload();
        var parsed = GenerationOutput.FromPayload(payload);

        Assert.IsLessThan(8000, Encoding.UTF8.GetByteCount(payload));
        Assert.IsNotNull(parsed);
        Assert.IsTrue(parsed.Truncated);
        Assert.IsTrue(content.EndsWith(parsed.Content, StringComparison.Ordinal));
    }

    [TestMethod]
    public void Payload_Garbage_IsIgnored()
    {
        Assert.IsNull(GenerationOutput.FromPayload("not json"));
    }
}
