using Honeyslop.Data.Entities;
using Honeyslop.Worker.Llm;
using Honeyslop.Worker.Llm.Internals;

namespace Honeyslop.Worker.Tests.Llm;

[TestClass]
public sealed class PromptBuilderTests
{
    [TestMethod]
    public void Build_NamesTheSoftwareAndRendersRequestAsHttp()
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
            $"Answer as: the JSON API of the website vps.example.test, served by {RequestProfiler.WebServer("vps.example.test")}\n" +
            "POST /api/auth?next=%2F HTTP/1.1\n" +
            "Host: vps.example.test\n" +
            "User-Agent: curl/8.0\n" +
            "Content-Type: application/json\n" +
            "\n" +
            """{"login":"admin"}""",
            prompt.User);
        StringAssert.Contains(prompt.System, "Reply with the JSON this API returns");
        StringAssert.Contains(prompt.System, "Reply with JSON: {\"status\"");
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

        Assert.AreEqual($"Answer as: the login page of the website a.test, served by {RequestProfiler.WebServer("a.test")}\nGET /login HTTP/1.1\nHost: a.test\nUser-Agent: Mozilla/5.0\nAccept: text/html\n\n", prompt.User);
        StringAssert.Contains(prompt.System, "Write all human-readable text in English.");
    }

    [TestMethod]
    [DataRow("/", "Reply with a complete HTML page")]
    [DataRow("/wp-login.php", "Reply with the page this software shows")]
    [DataRow("/HNAP1/", "Reply in the format this device's interface uses")]
    [DataRow("/.env", "Reply with the full content of the file")]
    [DataRow("/?cmd=id", "Reply with the response described in \"Answer as\".")]
    public void Build_GivesTheRuleOfTheKind(string target, string rule)
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = target
        }, true);

        StringAssert.StartsWith(prompt.System, "You are a web server.");
        StringAssert.Contains(prompt.System, "- " + rule);
        Assert.DoesNotContain("Example", prompt.System);
        foreach (var value in new[] { "nginx", "Apache", "IIS", "AKIA", "sk_live", "such as", "e.g.", "XML", "form fields", "vulnerab", "exploit", "unpatched", "scanner", "fake", "Server header" })
            Assert.DoesNotContain(value, prompt.System);
        Assert.DoesNotContain("{kind}", prompt.System);
    }

    [TestMethod]
    public void Build_SameKind_KeepsTheSystemPromptTheSame()
    {
        var first = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/.env"
        }, true);
        var second = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "b.test",
            Target = "/backup.sql"
        }, true);

        Assert.AreEqual(first.System, second.System);
    }

    [TestMethod]
    public void Build_RefusalsAllowed_LetsTheModelDenyAccess()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/"
        }, true);

        StringAssert.Contains(prompt.System, "or 404 with the site's not-found page when the path looks random");
        Assert.DoesNotContain("{status}", prompt.System);
    }

    [TestMethod]
    public void Build_RefusalsForbidden_DemandsContentForEveryPath()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/"
        }, false);


        StringAssert.Contains(prompt.System, "- Reply with status 200.\n");
        Assert.DoesNotContain("404", prompt.System);
        Assert.DoesNotContain("{status}", prompt.System);
    }

    [TestMethod]
    public void Build_FileWithRefusals_StillServesIt()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/.env"
        }, true);

        StringAssert.Contains(prompt.System, "- Reply with status 200, or 302 to a local path where the software redirects");
        StringAssert.Contains(prompt.User, "Answer as: the file /.env, readable on the website a.test");
        Assert.DoesNotContain("404", prompt.System);
        Assert.DoesNotContain("403", prompt.System);
        Assert.DoesNotContain("credentials", prompt.System);
    }

    [TestMethod]
    public void Build_WithGuidance_PassesTheOwnersRequestOnOneLine()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/SDK/webLanguage",
            Guidance = "Answer as a Hikvision camera.\r\nUse XML."
        }, true);

        StringAssert.Contains(prompt.System, "- The owner of this server asks for this response: Answer as a Hikvision camera. Use XML.\n\nReply with JSON");
        Assert.AreEqual("Answer as: Hikvision IP camera\nGET /SDK/webLanguage HTTP/1.1\nHost: a.test\n\n", prompt.User);
    }

    [TestMethod]
    public void Build_WithoutGuidance_LeavesNoPlaceholder()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/"
        }, true);

        Assert.DoesNotContain("{guidance}", prompt.System);
        Assert.DoesNotContain("owner of this server", prompt.System);
    }
}