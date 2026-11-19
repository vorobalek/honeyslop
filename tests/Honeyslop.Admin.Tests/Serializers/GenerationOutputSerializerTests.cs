using System.Text;
using Honeyslop.Common.Serializers;
using Honeyslop.Llm;
using Honeyslop.Llm.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Admin.Tests.Serializers;

[TestClass]
public sealed class GenerationOutputSerializerTests
{
    private static readonly IJsonSerializer<GenerationOutputDto> Serializer = new ServiceCollection()
        .AddLlm(x => x.Urls = [])
        .BuildServiceProvider()
        .GetRequiredService<IJsonSerializer<GenerationOutputDto>>();

    [TestMethod]
    public void Serialize_RoundTrips()
    {
        var output = new GenerationOutputDto(42, 3, "{\"status\":200,\"body\":\"<h1>Привет</h1>\"}");

        Assert.AreEqual(output, Serializer.Deserialize(Serializer.Serialize(output)));
    }

    [TestMethod]
    public void Serialize_LongOutput_KeepsTheTailWithinTheNotifyLimit()
    {
        var content = string.Concat(Enumerable.Range(0, 2000).Select(x => $"строка {x}\n"));
        var output = new GenerationOutputDto(7, 1024, content);

        var payload = Serializer.Serialize(output);
        var parsed = Serializer.Deserialize(payload);

        Assert.IsLessThan(8000, Encoding.UTF8.GetByteCount(payload));
        Assert.IsNotNull(parsed);
        Assert.IsTrue(parsed.Truncated);
        Assert.IsTrue(content.EndsWith(parsed.Content, StringComparison.Ordinal));
    }

    [TestMethod]
    public void Deserialize_Garbage_IsIgnored()
    {
        Assert.IsNull(Serializer.Deserialize("not json"));
    }
}