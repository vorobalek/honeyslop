using System.Collections.Concurrent;
using Honeyslop.Llm;
using Honeyslop.Llm.Contracts;
using Honeyslop.Llm.Exceptions;
using Honeyslop.Worker.Llm.Internals;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Honeyslop.Worker.Tests.Llm;

[TestClass]
public sealed class LlmDispatcherTests
{
    private static readonly Uri A = new("http://10.0.0.1:8080/");
    private static readonly Uri B = new("http://10.0.0.2:8080/");
    private static readonly Uri C = new("http://10.0.0.3:8080/");
    private static readonly LlmPrompt Prompt = new("system", "user");

    private static string NameOf(Uri endpoint)
    {
        return $"llm-{endpoint.Host.Split('.')[^1]}";
    }

    [TestMethod]
    public async Task CompleteAsync_PicksTheServerWithMostIdleSlots()
    {
        var sut = new Sut(A, B, C);
        sut.Idle[A] = 0;
        sut.Idle[B] = 2;
        sut.Idle[C] = 1;

        var completion = await sut.Dispatcher.CompleteAsync(Prompt, TimeSpan.FromMinutes(1), null, null, CancellationToken.None);

        Assert.AreEqual(NameOf(B), completion.Endpoint);
    }

    [TestMethod]
    public async Task CompleteAsync_ReportsTheServerBeforeTheGeneration()
    {
        var sut = new Sut(A, B);
        sut.Idle[A] = 0;
        sut.Idle[B] = 1;
        string? dispatchedTo = null;

        await sut.Dispatcher.CompleteAsync(Prompt, TimeSpan.FromMinutes(1), null, (name, _) =>
        {
            dispatchedTo = name;
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.AreEqual(NameOf(B), dispatchedTo);
    }

    [TestMethod]
    public async Task CompleteAsync_ConcurrentGenerations_SpreadAcrossServers()
    {
        var sut = new Sut(A, B, C);
        sut.Idle[A] = 1;
        sut.Idle[B] = 1;
        sut.Idle[C] = 1;
        sut.Hold = new TaskCompletionSource();

        var generations = Enumerable.Range(0, 3)
            .Select(_ => sut.Dispatcher.CompleteAsync(Prompt, TimeSpan.FromMinutes(1), null, null, CancellationToken.None))
            .ToArray();
        await sut.WaitForStartedAsync(3);
        sut.Hold.SetResult();
        var completions = await Task.WhenAll(generations);

        CollectionAssert.AreEquivalent(
            new[]
            {
                NameOf(A), NameOf(B), NameOf(C)
            },
            completions.Select(x => x.Endpoint).ToArray());
    }

    [TestMethod]
    public async Task CompleteAsync_EverySlotBusy_WaitsForAFreeOne()
    {
        var sut = new Sut(A, B);
        sut.Idle[A] = 0;
        sut.Idle[B] = 0;

        var generation = sut.Dispatcher.CompleteAsync(Prompt, TimeSpan.FromMinutes(1), null, null, CancellationToken.None);
        await Task.Delay(100);
        Assert.IsFalse(generation.IsCompleted);
        sut.Idle[B] = 1;
        var completion = await generation.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(NameOf(B), completion.Endpoint);
    }

    [TestMethod]
    public async Task CompleteAsync_FailedProbe_SkipsTheServer()
    {
        var sut = new Sut(A, B);
        sut.Idle[A] = null;
        sut.Idle[B] = 1;

        var completion = await sut.Dispatcher.CompleteAsync(Prompt, TimeSpan.FromMinutes(1), null, null, CancellationToken.None);

        Assert.AreEqual(NameOf(B), completion.Endpoint);
    }

    [TestMethod]
    public async Task CompleteAsync_ConnectionError_SkipsTheServerForAWhile()
    {
        var sut = new Sut(A, B);
        sut.Idle[A] = 2;
        sut.Idle[B] = 0;
        sut.Failing.Add(A);
        await Assert.ThrowsExactlyAsync<LlmUnavailableException>(() => sut.Dispatcher.CompleteAsync(Prompt, TimeSpan.FromMinutes(1), null, null, CancellationToken.None));

        sut.Idle[B] = 1;
        var completion = await sut.Dispatcher.CompleteAsync(Prompt, TimeSpan.FromMinutes(1), null, null, CancellationToken.None);

        Assert.AreEqual(NameOf(B), completion.Endpoint);
    }

    private sealed class Sut
    {
        private int _started;

        public Sut(params Uri[] endpoints)
        {
            var endpointProvider = new Mock<ILlmEndpointProvider>();
            endpointProvider
                .Setup(x => x.GetEndpointsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([.. endpoints.Select(x => new LlmEndpoint(NameOf(x), x))]);

            var client = new Mock<ILlmClient>();
            client
                .Setup(x => x.GetIdleSlotsAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Uri endpoint, CancellationToken _) => Idle.GetValueOrDefault(endpoint));
            client
                .Setup(x => x.CompleteAsync(It.IsAny<Uri>(), Prompt, It.IsAny<TimeSpan>(), It.IsAny<IProgress<LlmProgress>?>(), It.IsAny<CancellationToken>()))
                .Returns(async (Uri endpoint, LlmPrompt _, TimeSpan _, IProgress<LlmProgress>? _, CancellationToken _) =>
                {
                    Interlocked.Increment(ref _started);
                    if (Failing.Contains(endpoint))
                        throw new LlmUnavailableException("connection refused");
                    if (Hold is not null)
                        await Hold.Task;
                    return new LlmCompletion("{}", "stop", "honeyslop", 1, 1);
                });

            Dispatcher = new LlmDispatcher(
                endpointProvider.Object,
                client.Object,
                new OptionsWrapper<LlmOptions>(new LlmOptions
                {
                    SlotPollInterval = TimeSpan.FromMilliseconds(10),
                    UnhealthyCooldown = TimeSpan.FromMinutes(1),
                    DispatchGrace = TimeSpan.FromSeconds(30)
                }),
                TimeProvider.System,
                NullLogger<LlmDispatcher>.Instance);
        }

        public ConcurrentDictionary<Uri, int?> Idle { get; } = new();
        public ConcurrentBag<Uri> Failing { get; } = [];
        public TaskCompletionSource? Hold { get; set; }
        public LlmDispatcher Dispatcher { get; }

        public async Task WaitForStartedAsync(int count)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (Volatile.Read(ref _started) < count)
                await Task.Delay(10, timeout.Token);
        }
    }
}