using Honeyslop.Llm;
using Honeyslop.Data.Entities;
using Honeyslop.Trap.Llm;

namespace Honeyslop.Trap.Tests.Llm;

[TestClass]
public sealed class PromptBuilderTests
{
    [TestMethod]
    public void Build_RendersRequestAsHttp()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "POST",
            Host = "vps.example.test",
            Target = "/api/auth?next=%2F",
            RequestHeaders = "User-Agent: curl/8.0\nContent-Type: application/json\n",
            RequestBody = """{"login":"admin"}"""
        }, true);

        Assert.AreEqual(
            "POST /api/auth?next=%2F HTTP/1.1\n" +
            "Host: vps.example.test\n" +
            "User-Agent: curl/8.0\n" +
            "Content-Type: application/json\n" +
            "\n" +
            """{"login":"admin"}""",
            prompt.User);
        StringAssert.Contains(prompt.System, "Reply with JSON only");
    }

    [TestMethod]
    public void Build_LeavesOutAcceptLanguage()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/login",
            RequestHeaders = "User-Agent: Mozilla/5.0\naccept-language: zh-CN,zh;q=0.9\nAccept: text/html\n"
        }, true);

        Assert.AreEqual("GET /login HTTP/1.1\nHost: a.test\nUser-Agent: Mozilla/5.0\nAccept: text/html\n\n", prompt.User);
        StringAssert.Contains(prompt.System, "exists only in English");
    }

    [TestMethod]
    public void Build_WithoutHeadersAndBody_EndsWithEmptyLine()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = string.Empty,
            Target = "/"
        }, true);

        Assert.AreEqual("GET / HTTP/1.1\nHost: \n\n", prompt.User);
    }

    [TestMethod]
    public void Build_RefusalsAllowed_LetsTheModelDenyAccess()
    {
        var prompt = new PromptBuilder().Build(new Entry { Method = "GET", Host = "a.test", Target = "/" }, true);

        StringAssert.Contains(prompt.System, "401 or 403 for a login wall");
        Assert.DoesNotContain("Every request succeeds", prompt.System);
        Assert.DoesNotContain("{status_rules}", prompt.System);
    }

    [TestMethod]
    public void Build_RefusalsForbidden_DemandsContentForEveryPath()
    {
        var prompt = new PromptBuilder().Build(new Entry { Method = "GET", Host = "a.test", Target = "/" }, false);

        StringAssert.Contains(prompt.System, "Every request succeeds");
        Assert.DoesNotContain("401 or 403 for a login wall", prompt.System);
        Assert.DoesNotContain("{status_rules}", prompt.System);
    }
}
