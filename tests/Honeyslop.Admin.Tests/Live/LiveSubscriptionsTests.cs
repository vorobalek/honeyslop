using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using Honeyslop.Admin.Access;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Live;
using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Live.Contracts.Watches;
using Honeyslop.Admin.Live.Enums;
using Honeyslop.Admin.Live.Internals;
using Honeyslop.Admin.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Honeyslop.Admin.Tests.Live;

[TestClass]
public sealed class LiveSubscriptionsTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static readonly ClaimsPrincipal Anonymous = new();
    private readonly ConcurrentDictionary<string, string> _groupOf = new();

    private readonly Channel<(string Group, string Method, object?[] Arguments)> _sent = Channel.CreateUnbounded<(string, string, object?[])>();
    private readonly List<ClaimsPrincipal> _viewers = [];
    private LiveSubscriptions _subscriptions = null!;

    [TestInitialize]
    public void Initialize()
    {
        var viewerScope = new Mock<IViewerScope>();
        viewerScope.Setup(x => x.Use(It.IsAny<ClaimsPrincipal>())).Callback<ClaimsPrincipal>(user =>
        {
            lock (_viewers)
            {
                _viewers.Add(user);
            }
        });
        var services = new ServiceCollection()
            .AddSingleton(viewerScope.Object)
            .AddSingleton(Mock.Of<IViewBuilder>())
            .BuildServiceProvider();
        var sender = new Mock<ILiveSender>();
        sender
            .Setup(x => x.JoinAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((connectionId, group, _) => _groupOf[connectionId] = group)
            .Returns(Task.CompletedTask);
        sender
            .Setup(x => x.LeaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((connectionId, _, _) => _groupOf.TryRemove(connectionId, out _))
            .Returns(Task.CompletedTask);
        sender
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, object?[], CancellationToken>((group, method, arguments, _) =>
                _sent.Writer.WriteAsync((group, method, arguments)).AsTask());
        _subscriptions = new LiveSubscriptions(
            services.GetRequiredService<IServiceScopeFactory>(),
            sender.Object,
            TimeProvider.System,
            NullLogger<LiveSubscriptions>.Instance);
    }

    [TestMethod]
    public async Task Watch_SendsTheFirstViewToTheGroupAsTheWatchingUser()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "operator")], "test"));

        await _subscriptions.WatchAsync(Watch("a", "entries:[]", user, LiveInterest.Nothing, (_, _) => Task.FromResult<object?>("view")), CancellationToken.None);

        var message = await NextAsync();
        Assert.AreEqual(_groupOf["a"], message.Group);
        Assert.AreEqual(LiveView.Update, message.Method);
        CollectionAssert.AreEqual(new object?[]
        {
            "entries:[]", "view"
        }, message.Arguments);
        CollectionAssert.AreEqual(new[]
        {
            user
        }, _viewers);
    }

    [TestMethod]
    public async Task SameView_FromSeveralConnections_IsBuiltOnceForTheWholeGroup()
    {
        var builds = new int[1];
        await _subscriptions.WatchAsync(Watch("a", "entries:[]", Anonymous, new LiveInterest([LiveTopic.Entries]), Counting(builds, 0)), CancellationToken.None);
        await _subscriptions.WatchAsync(Watch("b", "entries:[]", Anonymous, new LiveInterest([LiveTopic.Entries]), Counting(builds, 0)), CancellationToken.None);
        await NextAsync(2);

        _subscriptions.Publish(new LiveChange(LiveTopic.Entries));
        var message = await NextAsync();

        Assert.AreEqual(_groupOf["a"], _groupOf["b"]);
        Assert.AreEqual(_groupOf["a"], message.Group);
        Assert.AreEqual(3, builds[0]);
        Assert.AreEqual(1, _subscriptions.Count);
        await Task.Delay(200);
        Assert.IsFalse(_sent.Reader.TryRead(out _));
    }

    [TestMethod]
    public async Task OtherAudienceOrArgument_GetsItsOwnGroup()
    {
        await _subscriptions.WatchAsync(Watch("a", "client", Anonymous, LiveInterest.Nothing, (_, _) => Task.FromResult<object?>(1), request: new ClientWatch("192.0.2.1")), CancellationToken.None);
        await _subscriptions.WatchAsync(Watch("b", "client", Anonymous, LiveInterest.Nothing, (_, _) => Task.FromResult<object?>(2), request: new ClientWatch("192.0.2.2")), CancellationToken.None);
        await _subscriptions.WatchAsync(Watch("c", "client", Anonymous, LiveInterest.Nothing, (_, _) => Task.FromResult<object?>(3), "operators", new ClientWatch("192.0.2.1")), CancellationToken.None);
        await NextAsync(3);

        Assert.AreEqual(3, _groupOf.Values.Distinct().Count());
        Assert.AreEqual(3, _subscriptions.Count);
    }

    [TestMethod]
    public async Task Publish_RefreshesOnlyTheViewsInterestedInTheChange()
    {
        var builds = new int[3];
        await _subscriptions.WatchAsync(Watch("a", "entries", Anonymous, new LiveInterest([LiveTopic.Entries]), Counting(builds, 0)), CancellationToken.None);
        await _subscriptions.WatchAsync(Watch("a", "requests", Anonymous, new LiveInterest([LiveTopic.Requests]), Counting(builds, 1)), CancellationToken.None);
        await _subscriptions.WatchAsync(Watch("b", "entry 7", Anonymous, new LiveInterest([LiveTopic.Entries, LiveTopic.Outputs], 7), Counting(builds, 2)), CancellationToken.None);
        await NextAsync(3);

        _subscriptions.Publish(new LiveChange(LiveTopic.Entries, 8));
        await NextAsync();
        _subscriptions.Publish(new LiveChange(LiveTopic.Outputs, 7));
        await NextAsync();

        CollectionAssert.AreEqual(new[]
        {
            2, 1, 2
        }, builds);
        Assert.IsFalse(_sent.Reader.TryRead(out _));
    }

    [TestMethod]
    public async Task Publish_WhileRefreshing_RefreshesOnceMoreAfterwards()
    {
        var builds = 0;
        var release = new TaskCompletionSource();
        await _subscriptions.WatchAsync(Watch("a", "requests", Anonymous, new LiveInterest([LiveTopic.Requests]), async (_, _) =>
        {
            if (Interlocked.Increment(ref builds) == 1)
                await release.Task;
            return builds;
        }), CancellationToken.None);

        for (var i = 0; i < 50; i++)
            _subscriptions.Publish(new LiveChange(LiveTopic.Requests));
        release.SetResult();

        await NextAsync(2);
        await Task.Delay(200);
        Assert.AreEqual(2, builds);
        Assert.IsFalse(_sent.Reader.TryRead(out _));
    }

    [TestMethod]
    public async Task Unwatch_AndDrop_StopTheUpdatesOnceTheGroupIsEmpty()
    {
        await _subscriptions.WatchAsync(Watch("a", "reports", Anonymous, new LiveInterest([LiveTopic.Reports]), (_, _) => Task.FromResult<object?>(1)), CancellationToken.None);
        await _subscriptions.WatchAsync(Watch("b", "reports", Anonymous, new LiveInterest([LiveTopic.Reports]), (_, _) => Task.FromResult<object?>(1)), CancellationToken.None);
        await _subscriptions.WatchAsync(Watch("b", "report 2", Anonymous, new LiveInterest([LiveTopic.Reports]), (_, _) => Task.FromResult<object?>(2)), CancellationToken.None);
        await NextAsync(3);

        await _subscriptions.UnwatchAsync("a", "reports", CancellationToken.None);
        Assert.AreEqual(2, _subscriptions.Count);
        _subscriptions.Drop("b");
        _subscriptions.Publish(new LiveChange(LiveTopic.Reports));

        Assert.AreEqual(0, _subscriptions.Count);
        await Task.Delay(200);
        Assert.IsFalse(_sent.Reader.TryRead(out _));
    }

    [TestMethod]
    public async Task Watch_WhenTheViewIsGone_SaysItIsMissing()
    {
        await _subscriptions.WatchAsync(Watch("a", "entry 3", Anonymous, LiveInterest.Nothing, (_, _) => Task.FromResult<object?>(null)), CancellationToken.None);

        var message = await NextAsync();
        Assert.AreEqual(LiveView.Missing, message.Method);
        CollectionAssert.AreEqual(new object?[]
        {
            "entry 3"
        }, message.Arguments);
    }

    [TestMethod]
    public async Task Watch_WhenAccessIsDenied_SendsTheReason()
    {
        await _subscriptions.WatchAsync(Watch("a", "settings", Anonymous, LiveInterest.Nothing, (_, _) => throw new AccessDeniedException("Sign in.")), CancellationToken.None);

        var message = await NextAsync();
        Assert.AreEqual(LiveView.Denied, message.Method);
        CollectionAssert.AreEqual(new object?[]
        {
            "settings", "Sign in."
        }, message.Arguments);
    }

    private static LiveWatch Watch(
        string connectionId,
        string key,
        ClaimsPrincipal user,
        LiveInterest interest,
        Func<IViewBuilder, CancellationToken, Task<object?>> build,
        string audience = "anonymous",
        WatchRequest? request = null)
    {
        return new LiveWatch(connectionId, key, audience, request ?? new GeneratorsWatch(), user, interest, build);
    }

    private static Func<IViewBuilder, CancellationToken, Task<object?>> Counting(int[] builds, int index)
    {
        return (_, _) => Task.FromResult<object?>(Interlocked.Increment(ref builds[index]));
    }

    private async Task<(string Group, string Method, object?[] Arguments)> NextAsync(int count = 1)
    {
        using var timeout = new CancellationTokenSource(Timeout);
        (string, string, object?[]) message = default;
        for (var i = 0; i < count; i++)
            message = await _sent.Reader.ReadAsync(timeout.Token);
        return message;
    }
}