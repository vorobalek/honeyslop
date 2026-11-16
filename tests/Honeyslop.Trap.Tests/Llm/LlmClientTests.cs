using System.Net;
using System.Text;
using Honeyslop.Llm;
using Microsoft.Extensions.Options;
using Moq;

namespace Honeyslop.Trap.Tests.Llm;

[TestClass]
public sealed class LlmClientTests
{
    private static readonly Uri Endpoint = new("http://10.0.0.1:8080/");
    private static readonly LlmPrompt Prompt = new("system", "user");

    [TestMethod]
    public async Task CompleteAsync_Stream_ReportsEveryTokenAndReturnsTheWholeOutput()
    {
        var reports = new List<LlmProgress>();
        var sut = Create(
            """
            data: {"model":"honeyslop","choices":[{"delta":{"role":"assistant","content":null},"finish_reason":null}]}

            data: {"model":"honeyslop","choices":[{"delta":{"content":"{\"status\":"},"finish_reason":null}]}

            data: {"model":"honeyslop","choices":[{"delta":{"content":"200}"},"finish_reason":null}]}

            data: {"model":"honeyslop","choices":[{"delta":{},"finish_reason":"stop"}]}

            data: {"model":"honeyslop","choices":[],"usage":{"prompt_tokens":812,"completion_tokens":2}}

            data: [DONE]

            """);

        var completion = await sut.CompleteAsync(Endpoint, Prompt, TimeSpan.FromMinutes(1), new Collector(reports), CancellationToken.None);

        Assert.AreEqual("{\"status\":200}", completion.Content);
        Assert.AreEqual("stop", completion.FinishReason);
        Assert.AreEqual("honeyslop", completion.Model);
        Assert.AreEqual(812, completion.PromptTokens);
        Assert.AreEqual(2, completion.CompletionTokens);
        CollectionAssert.AreEqual(
            new[] { new LlmProgress("{\"status\":", 1), new LlmProgress("{\"status\":200}", 2) },
            reports);
    }

    [TestMethod]
    public async Task CompleteAsync_StreamEndsBeforeTheFinish_IsUnavailable()
    {
        var sut = Create(
            """
            data: {"choices":[{"delta":{"content":"{\"sta"},"finish_reason":null}]}

            """);

        await Assert.ThrowsExactlyAsync<LlmUnavailableException>(() =>
            sut.CompleteAsync(Endpoint, Prompt, TimeSpan.FromMinutes(1), null, CancellationToken.None));
    }

    [TestMethod]
    public async Task CompleteAsync_ErrorEvent_FailsTheGeneration()
    {
        var sut = Create(
            """
            error: {"code":500,"message":"failed to parse grammar","type":"server_error"}

            """);

        var exception = await Assert.ThrowsExactlyAsync<LlmException>(() =>
            sut.CompleteAsync(Endpoint, Prompt, TimeSpan.FromMinutes(1), null, CancellationToken.None));
        StringAssert.Contains(exception.Message, "failed to parse grammar");
    }

    [TestMethod]
    public async Task CompleteAsync_ServerError_IsUnavailable()
    {
        var sut = Create("", HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsExactlyAsync<LlmUnavailableException>(() =>
            sut.CompleteAsync(Endpoint, Prompt, TimeSpan.FromMinutes(1), null, CancellationToken.None));
    }

    private static LlmClient Create(string stream, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(LlmClient.HttpClientName))
            .Returns(() => new HttpClient(new StreamHandler(stream, statusCode)));
        return new LlmClient(httpClientFactory.Object, new OptionsWrapper<LlmOptions>(new LlmOptions()));
    }

    private sealed class StreamHandler(string stream, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(stream, Encoding.UTF8, "text/event-stream")
            });
        }
    }

    private sealed class Collector(List<LlmProgress> reports) : IProgress<LlmProgress>
    {
        public void Report(LlmProgress value)
        {
            reports.Add(value);
        }
    }
}
