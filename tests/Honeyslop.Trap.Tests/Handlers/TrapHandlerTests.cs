using System.Net;
using System.Text;
using Honeyslop.Common;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Data.Requests;
using Honeyslop.Trap.Caching.Internals;
using Honeyslop.Trap.Generation.Internals;
using Honeyslop.Trap.Handlers.Internals;
using Honeyslop.Trap.Hits.Internals;
using Honeyslop.Trap.Options;
using Honeyslop.Trap.Requests;
using Honeyslop.Trap.Requests.Internals;
using Honeyslop.Trap.Responses;
using Honeyslop.Trap.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Honeyslop.Trap.Tests.Handlers;

[TestClass]
public sealed class TrapHandlerTests
{
    [TestMethod]
    public async Task HandleAsync_BinaryFile_RespondsNotFoundWithoutStorage()
    {
        using var sut = new Sut();
        var context = Request("GET", "/favicon.ico");

        await sut.Handler.HandleAsync(context);

        Assert.AreEqual(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.AreEqual("nginx", context.Response.Headers.Server.ToString());
        StringAssert.Contains(Body(context), "<center>nginx</center>");
        sut.Repository.Verify(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        var record = sut.LoggedRecord();
        Assert.AreEqual(RequestOutcome.Static, record.Outcome);
        Assert.AreEqual("/favicon.ico", record.Target);
        Assert.AreEqual("203.0.113.7", record.ClientIp);
        Assert.AreEqual(404, record.ResponseStatus);
        Assert.IsNull(record.EntryId);
    }

    [TestMethod]
    public async Task HandleAsync_AnyRequest_IsLoggedWithHeadersButWithoutCookieValues()
    {
        using var sut = new Sut();
        var context = Request("POST", "/x.zip", "user=admin&pass=123456");
        context.Request.Headers.Cookie = "auth_session=secret-value; theme=dark";
        context.Request.Headers.Authorization = "Basic YWRtaW46YWRtaW4=";

        await sut.Handler.HandleAsync(context);

        var record = sut.LoggedRecord();
        Assert.AreEqual("POST", record.Method);
        Assert.AreEqual("kms.example.test", record.Host);
        Assert.AreEqual("user=admin&pass=123456", record.Body);
        StringAssert.Contains(record.Headers, "Cookie: auth_session=…; theme=…\n");
        StringAssert.Contains(record.Headers, "Authorization: Basic YWRtaW46YWRtaW4=\n");
        Assert.DoesNotContain("secret-value", record.Headers!);
    }

    [TestMethod]
    public async Task HandleAsync_WaitingRequest_IsLoggedFirstAndUpdatedWhenAnswered()
    {
        using var sut = new Sut();
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PendingEntry(5));

        var context = Request("GET", "/admin");
        var handling = sut.Handler.HandleAsync(context);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        RequestLogItem? waiting = null;
        while (waiting is null && !timeout.IsCancellationRequested)
            if (!sut.Log.TryRead(out waiting))
                await Task.Delay(10, timeout.Token);
        Assert.IsNotNull(waiting);
        Assert.IsFalse(waiting.IsUpdate);
        Assert.AreEqual(RequestOutcome.Waiting, waiting.Record.Outcome);
        Assert.AreEqual(5, waiting.Record.EntryId);

        while (!handling.IsCompleted)
        {
            sut.Watcher.Complete(5, Generated(5, 200, "admin panel"));
            await Task.Delay(10, timeout.Token);
        }

        await handling;
        Assert.IsTrue(sut.Log.TryRead(out var answered));
        Assert.IsNotNull(answered);
        Assert.IsTrue(answered.IsUpdate);
        Assert.AreSame(waiting.Record, answered.Record);
        Assert.AreEqual(RequestOutcome.Generated, answered.Record.Outcome);
        Assert.AreEqual(200, answered.Record.ResponseStatus);
    }

    [TestMethod]
    public async Task HandleAsync_ReadyEntry_ServesStoredResponseAndCachesIt()
    {
        using var sut = new Sut();
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(
                It.Is<byte[]>(hash => hash.SequenceEqual(EntryKey.Create("GET", "kms.example.test", "/.env").Hash)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReadyEntry(7));

        var first = Request("GET", "/.env");
        var second = Request("HEAD", "/.env");
        await sut.Handler.HandleAsync(first);
        await sut.Handler.HandleAsync(second);

        Assert.AreEqual(StatusCodes.Status200OK, first.Response.StatusCode);
        Assert.AreEqual("APP_KEY=base64:fake", Body(first));
        Assert.AreEqual("text/plain; charset=utf-8", first.Response.ContentType);
        Assert.AreEqual("sandbox; default-src 'none'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; font-src 'self' data:",
            first.Response.Headers.ContentSecurityPolicy.ToString());
        Assert.AreEqual(string.Empty, Body(second));
        Assert.AreEqual(19, second.Response.ContentLength);
        CollectionAssert.AreEqual(new[]
        {
            7L
        }, sut.Hits.Drain().Where(x => x.Count == 2).Select(x => x.EntryId).ToArray());
        sut.Repository.Verify(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_EntryWithItsOwnDelay_WaitsThatLongBeforeAnswering()
    {
        var time = new DelayRecorder();
        using var sut = new Sut(time, x => x.ResponseDelay = TimeSpan.FromSeconds(1));
        sut.CurrentSettings = sut.CurrentSettings with { ResponseDelayMilliseconds = 10_000 };
        var entry = ReadyEntry(7);
        entry.ResponseDelayMilliseconds = 3_250;
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var context = Request("GET", "/.env");
        await sut.Handler.HandleAsync(context);

        Assert.AreEqual("APP_KEY=base64:fake", Body(context));
        CollectionAssert.AreEqual(new[] { TimeSpan.FromMilliseconds(3_250) }, time.Delays);
    }

    [TestMethod]
    [DataRow(1_000, 2_250)]
    [DataRow(3_250, 0)]
    [DataRow(5_700, 0)]
    public async Task HandleAsync_TimeAlreadySpentOnTheRequest_CountsTowardsTheDelay(int spentMs, int waitedMs)
    {
        var time = new DelayRecorder();
        using var sut = new Sut(time);
        var entry = ReadyEntry(7);
        entry.ResponseDelayMilliseconds = 3_250;
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback(() => time.Advance(TimeSpan.FromMilliseconds(spentMs)))
            .ReturnsAsync(entry);

        await sut.Handler.HandleAsync(Request("GET", "/.env"));

        CollectionAssert.AreEqual(waitedMs == 0 ? Array.Empty<TimeSpan>() : new[] { TimeSpan.FromMilliseconds(waitedMs) }, time.Delays);
    }

    [TestMethod]
    public async Task HandleAsync_ClientWithADelay_WaitsThatLongEvenIfTheEntryHasItsOwn()
    {
        var time = new DelayRecorder();
        using var sut = new Sut(time, x => x.ResponseDelay = TimeSpan.FromSeconds(1));
        sut.CurrentSettings = sut.CurrentSettings with { ResponseDelayMilliseconds = 10_000 };
        sut.ClientDelays
            .Setup(x => x.GetAsync("203.0.113.7", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(750));
        var entry = ReadyEntry(7);
        entry.ResponseDelayMilliseconds = 3_250;
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var context = Request("GET", "/.env");
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:203.0.113.7");
        await sut.Handler.HandleAsync(context);

        CollectionAssert.AreEqual(new[] { TimeSpan.FromMilliseconds(750) }, time.Delays);
    }

    [TestMethod]
    [DataRow(4, 2, 4)]
    [DataRow(-1, 2, 2)]
    [DataRow(-1, 0, 0)]
    public async Task HandleAsync_EntryWithoutADelay_TakesTheSettingThenTheEnvironment(int setting, int environment, int expected)
    {
        var time = new DelayRecorder();
        using var sut = new Sut(time, x => x.ResponseDelay = TimeSpan.FromSeconds(environment));
        sut.CurrentSettings = sut.CurrentSettings with { ResponseDelayMilliseconds = setting < 0 ? null : setting * 1000 };
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReadyEntry(7));

        await sut.Handler.HandleAsync(Request("GET", "/.env"));

        CollectionAssert.AreEqual(expected == 0 ? Array.Empty<TimeSpan>() : new[] { TimeSpan.FromSeconds(expected) }, time.Delays);
    }

    [TestMethod]
    public async Task HandleAsync_StaticFile_IsNotDelayed()
    {
        var time = new DelayRecorder();
        using var sut = new Sut(time, x => x.ResponseDelay = TimeSpan.FromSeconds(5));

        await sut.Handler.HandleAsync(Request("GET", "/favicon.ico"));

        Assert.IsEmpty(time.Delays);
    }

    [TestMethod]
    public async Task HandleAsync_NewPath_QueuesTheEntryAndServesItsGeneration()
    {
        using var sut = new Sut();
        Entry? queued = null;
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entry?)null);
        sut.Repository
            .Setup(x => x.TryAddAsync(It.IsAny<Entry>(), It.IsAny<CancellationToken>()))
            .Callback<Entry, CancellationToken>((entry, _) =>
            {
                entry.Id = 42;
                queued = entry;
            })
            .ReturnsAsync(true);

        var context = Request("POST", "/api/login?next=%2F", "{\"login\":\"admin\"}");
        await sut.HandleAndCompleteAsync(context, 42, Generated(42, 201, "{\"ok\":false}"));

        Assert.AreEqual(201, context.Response.StatusCode);
        Assert.AreEqual("{\"ok\":false}", Body(context));
        Assert.IsNotNull(queued);
        Assert.AreEqual(EntryStatus.Pending, queued.Status);
        Assert.IsNull(queued.ClaimedBy);
        Assert.IsNull(queued.ClaimedAt);
        Assert.IsNotNull(queued.RequestedAt);
        Assert.AreEqual("POST", queued.Method);
        Assert.AreEqual("kms.example.test", queued.Host);
        Assert.AreEqual("/api/login?next=%2F", queued.Target);
        Assert.AreEqual("203.0.113.7", queued.ClientIp);
        Assert.AreEqual("zgrab/0.x", queued.UserAgent);
        Assert.AreEqual("{\"login\":\"admin\"}", queued.RequestBody);
        StringAssert.Contains(queued.RequestHeaders, "User-Agent: zgrab/0.x\n");
        CollectionAssert.AreEqual(EntryKey.Create("POST", "kms.example.test", "/api/login?next=%2F").Hash, queued.KeyHash);
        var record = sut.LoggedRecord();
        Assert.AreEqual(RequestOutcome.Generated, record.Outcome);
        Assert.AreEqual(42, record.EntryId);
    }

    [TestMethod]
    public async Task HandleAsync_LostInsertRace_WaitsForTheExistingEntry()
    {
        using var sut = new Sut();
        sut.Repository
            .SetupSequence(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entry?)null)
            .ReturnsAsync(PendingEntry(77));
        sut.Repository
            .Setup(x => x.TryAddAsync(It.IsAny<Entry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var context = Request("GET", "/admin");
        await sut.HandleAndCompleteAsync(context, 77, Generated(77, 200, "panel"));

        Assert.AreEqual(200, context.Response.StatusCode);
        Assert.AreEqual("panel", Body(context));
    }

    [TestMethod]
    public async Task HandleAsync_EntryGeneratedElsewhere_WaitsForIt()
    {
        using var sut = new Sut();
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PendingEntry(5, "other-instance"));

        var context = Request("GET", "/admin");
        await sut.HandleAndCompleteAsync(context, 5, Generated(5, 200, "admin panel"));

        Assert.AreEqual(200, context.Response.StatusCode);
        Assert.AreEqual("admin panel", Body(context));
        sut.Repository.Verify(x => x.TryAddAsync(It.IsAny<Entry>(), It.IsAny<CancellationToken>()), Times.Never);
        sut.Repository.Verify(x => x.TouchRequestedAsync(5, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_FailedEntryWithAttemptsLeft_WaitsForTheRetry()
    {
        using var sut = new Sut();
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entry
            {
                Id = 9,
                Status = EntryStatus.Failed,
                Attempts = 1
            });

        var context = Request("GET", "/panel");
        await sut.HandleAndCompleteAsync(context, 9, Generated(9, 200, "<html>panel</html>"));

        Assert.AreEqual(200, context.Response.StatusCode);
        Assert.AreEqual("<html>panel</html>", Body(context));
    }

    [TestMethod]
    public async Task HandleAsync_FailedEntryOutOfAttempts_RespondsFallbackAtOnce()
    {
        using var sut = new Sut();
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entry
            {
                Id = 9,
                Status = EntryStatus.Failed,
                Attempts = 3
            });

        var context = Request("GET", "/panel");
        await sut.Handler.HandleAsync(context);

        Assert.AreEqual(StatusCodes.Status404NotFound, context.Response.StatusCode);
        var record = sut.LoggedRecord();
        Assert.AreEqual(RequestOutcome.Fallback, record.Outcome);
        Assert.IsLessThan(1000, record.DurationMs);
    }

    [TestMethod]
    public async Task HandleAsync_GenerationDisabled_RespondsFallbackWithoutQueueing()
    {
        using var sut = new Sut();
        sut.CurrentSettings = sut.CurrentSettings with
        {
            GenerationEnabled = false
        };
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entry?)null);

        var context = Request("GET", "/wp-login.php");
        await sut.Handler.HandleAsync(context);

        Assert.AreEqual(StatusCodes.Status404NotFound, context.Response.StatusCode);
        sut.Repository.Verify(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        sut.Repository.Verify(x => x.TryAddAsync(It.IsAny<Entry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_NotReadyInTime_RespondsFallback()
    {
        using var sut = new Sut();
        sut.CurrentSettings = sut.CurrentSettings with
        {
            GenerationWait = TimeSpan.FromMilliseconds(300)
        };
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PendingEntry(5));

        var context = Request("GET", "/admin");
        await sut.Handler.HandleAsync(context);

        Assert.AreEqual(StatusCodes.Status404NotFound, context.Response.StatusCode);
        var record = sut.LoggedRecord();
        Assert.AreEqual(RequestOutcome.Fallback, record.Outcome);
    }

    [TestMethod]
    public async Task HandleAsync_ClientLeavesWhileWaiting_IsLoggedAsAbandoned()
    {
        using var sut = new Sut();
        sut.Repository
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PendingEntry(5));
        using var aborted = new CancellationTokenSource();
        var context = Request("GET", "/admin/login");
        context.RequestAborted = aborted.Token;

        var handling = sut.Handler.HandleAsync(context);
        await aborted.CancelAsync();
        await handling;

        var record = sut.LoggedRecord();
        Assert.AreEqual(RequestOutcome.Abandoned, record.Outcome);
        Assert.AreEqual(499, record.ResponseStatus);
    }

    private static Entry ReadyEntry(long id)
    {
        return new Entry
        {
            Id = id,
            Method = "GET",
            Host = "kms.example.test",
            Target = "/.env",
            Status = EntryStatus.Ready,
            ResponseStatus = 200,
            ResponseHeaders =
            [
                new ResponseHeader
                {
                    Name = "Content-Type",
                    Value = "text/plain; charset=utf-8"
                }
            ],
            ResponseBody = "APP_KEY=base64:fake"
        };
    }

    private static Entry PendingEntry(long id, string? claimedBy = null)
    {
        return new Entry
        {
            Id = id,
            Status = EntryStatus.Pending,
            ClaimedBy = claimedBy,
            ClaimedAt = claimedBy is null ? null : DateTimeOffset.UtcNow
        };
    }

    private static CachedResponse Generated(long entryId, int status, string body)
    {
        return CachedResponse.From(entryId, new GeneratedResponse(
            status,
            [
                new ResponseHeader
                {
                    Name = "Content-Type",
                    Value = "application/json"
                }
            ],
            body));
    }

    private static DefaultHttpContext Request(string method, string target, string? body = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Host = new HostString("Kms.Example.Test");
        context.Features.Get<IHttpRequestFeature>()!.RawTarget = target;
        context.Request.Headers.UserAgent = "zgrab/0.x";
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        if (body is not null)
        {
            context.Request.ContentType = "application/json";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        }

        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string Body(HttpContext context)
    {
        return Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
    }

    private sealed class DelayRecorder : TimeProvider
    {
        private readonly List<TimeSpan> _delays = [];
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            return Interlocked.Read(ref _timestamp);
        }

        public void Advance(TimeSpan elapsed)
        {
            Interlocked.Add(ref _timestamp, elapsed.Ticks);
        }

        public TimeSpan[] Delays
        {
            get
            {
                lock (_delays)
                    return [.. _delays];
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            lock (_delays)
                _delays.Add(dueTime);
            return base.CreateTimer(callback, state, TimeSpan.Zero, period);
        }
    }

    private sealed class Sut : IDisposable
    {
        public Sut(TimeProvider? time = null, Action<TrapOptions>? configure = null)
        {
            var options = TestOptions.Trap(configure);
            Settings
                .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CurrentSettings);
            var globalCancellationTokenSource = new Mock<IGlobalCancellationTokenSource>();
            globalCancellationTokenSource
                .SetupGet(x => x.Token)
                .Returns(CancellationToken.None);
            Cache = new ResponseCache(options);
            Cache.Enable();
            Watcher = new EntryWatcher(
                new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
                options,
                TimeProvider.System,
                NullLogger<EntryWatcher>.Instance);
            var serverTime = new Mock<IServerTimeRepository>();
            serverTime.Setup(x => x.GetNowAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => DateTimeOffset.UtcNow);
            Handler = new TrapHandler(
                new RequestClassifier(options),
                Cache,
                Repository.Object,
                serverTime.Object,
                new GenerationSignal(options),
                Watcher,
                Hits,
                Settings.Object,
                ClientDelays.Object,
                Log,
                globalCancellationTokenSource.Object,
                options,
                time ?? TimeProvider.System,
                NullLogger<TrapHandler>.Instance);
        }

        public Mock<IEntryRepository> Repository { get; } = CreateRepository();
        public Mock<ISettingsProvider> Settings { get; } = new();
        public Mock<IClientDelays> ClientDelays { get; } = new();

        public HoneyslopSettings CurrentSettings { get; set; } = HoneyslopSettings.Default with
        {
            GenerationWait = TimeSpan.FromSeconds(5)
        };

        public HitCounter Hits { get; } = new();
        public RequestLog Log { get; } = new();
        public ResponseCache Cache { get; }
        public EntryWatcher Watcher { get; }
        public TrapHandler Handler { get; }

        public void Dispose()
        {
            Cache.Dispose();
            Watcher.Dispose();
        }

        public RequestRecord LoggedRecord()
        {
            Assert.IsTrue(Log.TryRead(out var item));
            Assert.IsNotNull(item);
            return item.Record;
        }

        private static Mock<IEntryRepository> CreateRepository()
        {
            var repository = new Mock<IEntryRepository>(MockBehavior.Strict);
            repository
                .Setup(x => x.TouchRequestedAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return repository;
        }

        public async Task HandleAndCompleteAsync(HttpContext context, long entryId, CachedResponse response)
        {
            var handling = Handler.HandleAsync(context);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (!handling.IsCompleted)
            {
                Watcher.Complete(entryId, response);
                await Task.Delay(10, timeout.Token);
            }

            await handling;
        }
    }
}