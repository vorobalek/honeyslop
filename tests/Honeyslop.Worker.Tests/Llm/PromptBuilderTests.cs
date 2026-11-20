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
        StringAssert.Contains(prompt.System, "Reply with what this API returns, in its own format");
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
    [DataRow("/.env", "The server serves this file: reply with its full content in its real format.")]
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
        foreach (var value in new[] { "nginx", "Apache", "IIS", "AKIA", "sk_live", "such as", "e.g.", "XML", "JSON this", "form fields", "vulnerab", "exploit", "unpatched", "scanner", "fake", "Server header" })
            Assert.DoesNotContain(value, prompt.System);
        Assert.DoesNotContain("{kind}", prompt.System);
    }

    [TestMethod]
    [DataRow("/.env", true)]
    [DataRow("/api/v1/keys", true)]
    [DataRow("/?cmd=id", true)]
    [DataRow("/", false)]
    [DataRow("/wp-login.php", false)]
    [DataRow("/HNAP1/", false)]
    public void Build_AsksForRealLookingSecretsOnlyWhereTheSoftwareShowsThem(string target, bool asked)
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = target
        }, true);

        Assert.AreEqual(asked, prompt.System.Contains("Passwords, tokens and keys look real", StringComparison.Ordinal));
        Assert.DoesNotContain("{secrets}", prompt.System);
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
            Target = "/.git/config"
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
            Target = "/catalog/shoes"
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

        StringAssert.Contains(prompt.System, "- Reply with status 200.\n");
        StringAssert.Contains(prompt.User, "Answer as: the file /.env, readable on the website a.test");
        Assert.DoesNotContain("404", prompt.System);
        Assert.DoesNotContain("403", prompt.System);
        Assert.DoesNotContain("credentials", prompt.System);
    }

    [TestMethod]
    [DataRow("Model refused a path it has to answer: 403", "This path is served: reply with status 200 and its content.")]
    [DataRow("Model output gives itself away: This is a sample", "Send only what the software itself sends.")]
    [DataRow("Model output has a placeholder: lorem ipsum", "Fill every value with something specific and realistic.")]
    [DataRow("Model output is not valid JSON: '}' is invalid", "Reply with JSON in exactly the required shape.")]
    [DataRow("Model output was cut at the token limit.", "Keep the body well under 1500 characters.")]
    public void Build_AfterARejectedReply_SaysWhatToDoWithoutQuotingIt(string error, string hint)
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/.env",
            Attempts = 1,
            Error = error
        }, true);

        StringAssert.Contains(prompt.System, "\n- The last reply to this request was turned down. " + hint + "\n\nReply with JSON");
        Assert.DoesNotContain(error[(error.IndexOf(':') + 1)..].Trim(), prompt.System.Replace(hint, string.Empty));
    }

    [TestMethod]
    [DataRow(0, "Model refused a path it has to answer: 403")]
    [DataRow(1, "Model server 10.0.0.5:8080 is unavailable")]
    [DataRow(1, null)]
    public void Build_WithoutARejectedReply_GivesNoHint(int attempts, string? error)
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/.env",
            Attempts = attempts,
            Error = error
        }, true);

        Assert.DoesNotContain("turned down", prompt.System);
    }

    [TestMethod]
    [DataRow("/.git/HEAD", true, new[] { 200 })]
    [DataRow("/.git/HEAD", false, new[] { 200 })]
    [DataRow("/", true, new[] { 200 })]
    [DataRow("/catalog/shoes", true, new[] { 200, 404 })]
    [DataRow("/catalog/shoes", false, new[] { 200 })]
    [DataRow("/wp-login.php", true, new[] { 200, 302 })]
    [DataRow("/HNAP1/", false, new[] { 200 })]
    [DataRow("/k3j4h5x9.txt", true, new[] { 200, 404 })]
    [DataRow("/k3j4h5x9.txt", false, new[] { 200 })]
    [DataRow("/.git/index", true, new[] { 200, 404 })]
    [DataRow("/.env", true, new[] { 200 })]
    public void Build_LimitsTheStatusByKind(string target, bool refusals, int[] statuses)
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = target
        }, refusals);

        CollectionAssert.AreEqual(statuses, prompt.Statuses!.ToArray());
    }

    [TestMethod]
    public void Build_WithGuidance_LeavesTheStatusToTheOwner()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/.git/HEAD",
            Guidance = "Answer 403 like nginx does for hidden files."
        }, true);

        Assert.IsNull(prompt.Statuses);
    }

    [TestMethod]
    public void Build_File_IsServed()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/.git/config"
        }, true);

        StringAssert.Contains(prompt.System, "- The server serves this file: reply with its full content in its real format.\n");
        StringAssert.Contains(prompt.System, "- Reply with status 200.\n");
    }

    [TestMethod]
    public void Build_FileThatMayNotExist_MayGetANotFound()
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = "/k3j4h5x9.txt"
        }, true);

        StringAssert.Contains(prompt.System, "- Reply with the full content of the file in its real format.\n");
        StringAssert.Contains(prompt.System, "- Reply with status 200, or 404 with the server's not-found page when the file name looks random.\n");
        Assert.DoesNotContain("The server serves this file", prompt.System);
    }

    [TestMethod]
    [DataRow("/", true)]
    [DataRow("/", false)]
    [DataRow("/catalog/shoes", true)]
    [DataRow("/.env", true)]
    [DataRow("/k3j4h5x9.txt", true)]
    [DataRow("/wp-login.php", true)]
    [DataRow("/HNAP1/", false)]
    public void Build_StatusRule_NamesExactlyTheStatusesTheSchemaAllows(string target, bool refusals)
    {
        var prompt = new PromptBuilder().Build(new Entry
        {
            Method = "GET",
            Host = "a.test",
            Target = target
        }, refusals);

        var rule = prompt.System.Split('\n').Single(x => x.StartsWith("- Reply with status", StringComparison.Ordinal));
        foreach (var status in new[] { 200, 302, 404 })
            Assert.AreEqual(prompt.Statuses!.Contains(status), rule.Contains(status.ToString()), $"{status} in {rule}");
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