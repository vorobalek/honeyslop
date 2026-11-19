using Honeyslop.Trap.Requests;
using Honeyslop.Trap.Requests.Internals;

namespace Honeyslop.Trap.Tests.Requests;

[TestClass]
public sealed class RequestClassifierTests
{
    private readonly RequestClassifier _classifier = new(TestOptions.Trap(x => x.MaxTargetLength = 64));

    [TestMethod]
    [DataRow("/favicon.ico")]
    [DataRow("/images/logo.PNG")]
    [DataRow("/backup.tar.gz")]
    [DataRow("/report.pdf?download=1")]
    [DataRow("/fonts/inter.woff2")]
    [DataRow("/favicon%2Eico")]
    [DataRow("/db/site.sqlite3")]
    public void Classify_BinaryFile_ReturnsStaticFile(string target)
    {
        var classification = _classifier.Classify("GET", target);

        Assert.AreEqual(ClassificationKind.StaticFile, classification.Kind);
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("/.env")]
    [DataRow("/.git/config")]
    [DataRow("/wp-login.php")]
    [DataRow("/api/v1/users")]
    [DataRow("/backup.sql")]
    [DataRow("/config.json?logo=a.png")]
    [DataRow("/cgi-bin/")]
    public void Classify_TextualPath_ReturnsGenerate(string target)
    {
        var classification = _classifier.Classify("GET", target);

        Assert.AreEqual(ClassificationKind.Generate, classification.Kind);
    }

    [TestMethod]
    public void Classify_Head_IsGeneratedAsGet()
    {
        var classification = _classifier.Classify("HEAD", "/admin");

        Assert.AreEqual(new Classification(ClassificationKind.Generate, "GET"), classification);
    }

    [TestMethod]
    [DataRow("post", "POST")]
    [DataRow("PUT", "PUT")]
    [DataRow("PATCH", "PATCH")]
    [DataRow("DELETE", "DELETE")]
    public void Classify_BodyMethods_AreGeneratedWithTheirOwnKey(string method, string expected)
    {
        var classification = _classifier.Classify(method, "/api/login");

        Assert.AreEqual(new Classification(ClassificationKind.Generate, expected), classification);
    }

    [TestMethod]
    [DataRow("CONNECT")]
    [DataRow("OPTIONS")]
    [DataRow("TRACE")]
    [DataRow("NSJV")]
    public void Classify_OtherMethods_ReturnsMethodNotAllowed(string method)
    {
        var classification = _classifier.Classify(method, "/");

        Assert.AreEqual(ClassificationKind.MethodNotAllowed, classification.Kind);
    }

    [TestMethod]
    public void Classify_LongTarget_ReturnsUriTooLong()
    {
        var classification = _classifier.Classify("GET", "/" + new string('a', 64));

        Assert.AreEqual(ClassificationKind.UriTooLong, classification.Kind);
    }

    [TestMethod]
    [DataRow("http://example.test/")]
    [DataRow("*")]
    [DataRow("")]
    public void Classify_NonOriginForm_ReturnsBadRequest(string target)
    {
        var classification = _classifier.Classify("GET", target);

        Assert.AreEqual(ClassificationKind.BadRequest, classification.Kind);
    }

    [TestMethod]
    public void Classify_ConfiguredExtensions_ReplaceDefaults()
    {
        var classifier = new RequestClassifier(TestOptions.Trap(x => x.StaticExtensions = StaticExtensions.Parse(".txt, LOG")));

        Assert.AreEqual(ClassificationKind.StaticFile, classifier.Classify("GET", "/robots.txt").Kind);
        Assert.AreEqual(ClassificationKind.StaticFile, classifier.Classify("GET", "/error.log").Kind);
        Assert.AreEqual(ClassificationKind.Generate, classifier.Classify("GET", "/favicon.ico").Kind);
    }
}