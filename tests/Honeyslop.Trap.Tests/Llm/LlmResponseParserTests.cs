using Honeyslop.Llm.Exceptions;
using Honeyslop.Trap.Llm;

namespace Honeyslop.Trap.Tests.Llm;

[TestClass]
public sealed class LlmResponseParserTests
{
    [TestMethod]
    public void Parse_ValidJson_ReturnsResponse()
    {
        var response = LlmResponseParser.Parse(
            """{"status": 403, "headers": {"Server": "nginx", "Content-Type": "text/html"}, "body": "<h1>Forbidden</h1>"}""");

        Assert.AreEqual(403, response.Status);
        Assert.AreEqual("<h1>Forbidden</h1>", response.Body);
        CollectionAssert.AreEqual(
            new[] { "Server: nginx", "Content-Type: text/html" },
            response.Headers.Select(x => $"{x.Name}: {x.Value}").ToArray());
    }

    [TestMethod]
    public void Parse_CodeFence_IsIgnored()
    {
        var response = LlmResponseParser.Parse(
            """
            ```json
            {"status": 200, "headers": {}, "body": "ok"}
            ```
            """);

        Assert.AreEqual("ok", response.Body);
    }

    [TestMethod]
    public void Parse_NonStringHeader_IsSkipped()
    {
        var response = LlmResponseParser.Parse("""{"status": 200, "headers": {"X-Count": 5, "Server": "Apache"}, "body": ""}""");

        Assert.AreEqual(1, response.Headers.Count);
        Assert.AreEqual("Server", response.Headers[0].Name);
    }

    [TestMethod]
    [DataRow("not json")]
    [DataRow("[]")]
    [DataRow("""{"headers": {}, "body": "x"}""")]
    [DataRow("""{"status": "200", "headers": {}, "body": "x"}""")]
    [DataRow("""{"status": 200.5, "headers": {}, "body": "x"}""")]
    [DataRow("""{"status": 200, "headers": {}}""")]
    [DataRow("{\"status\": 200, \"headers\": {}, \"body\": \"x\\\"")]
    public void Parse_InvalidOutput_Throws(string content)
    {
        Assert.ThrowsExactly<LlmException>(() => LlmResponseParser.Parse(content));
    }
}
