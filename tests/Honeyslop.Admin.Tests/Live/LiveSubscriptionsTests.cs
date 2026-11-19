using System.Security.Claims;
using System.Threading.Channels;
using Honeyslop.Admin.Access;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Live;
using Honeyslop.Admin.Live.Contracts;
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

    private readonly Channel<(string ConnectionId, string Method, object?[] Arguments)> _sent = Channel.CreateUnbounded<(string, string, object?[])>();
    private readonly List<ClaimsPrincipal> _viewers = [];
    private LiveSubscriptions _subscriptions = null!;

    [TestInitialize]
    public void Initialize()
    {
        var viewerScope = new Mock<IViewerScope>();
        viewerScope.Setup(x => x.Use(It.IsAny<ClaimsPrincipal>())).Callback<ClaimsPrincipal>(user =>
        {
            lock (_viewers)
                _viewers.Add(user);
        });
        var services = new ServiceCollection()
            .AddSingleton(viewerScope.Object)
            .AddSingleton(Mock.Of<IViewBuilder>())
            .BuildServiceProvider();
        var sender = new Mock<ILiveSender>();
        sender
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, object?[], CancellationToken>((connectionId, method, arguments, _) =>
                _sent.Writer.WriteAsync((connectionId, method, arguments)).AsTask());
        _subscriptions = new LiveSubscriptions(
            services.GetRequiredService<IServiceScopeFactory>(),
            sender.Object,
            TimeProvider.System,
            NullLogger<LiveSubscriptions>.Instance);
    }

    [TestMethod]
    public async Task Watch_SendsTheFirstViewAsTheSubscribedUser()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "operator")], "test"));

        _subscriptions.Watch("a", 1, user, LiveInterest.Nothing, (_, _) => Task.FromResult<object?>("view"));

        var message = await NextAsync();
        Assert.AreEqual("a", message.ConnectionId);
        Assert.AreEqual(LiveSubscription.Update, message.Method);
        CollectionAssert.AreEqual(new object?[] { 1, "view" }, message.Arguments);
        CollectionAssert.AreEqual(new[] { user }, _viewers);
    }

    [TestMethod]
    public async Task Publish_RefreshesOnlyTheSubscriptionsInterestedInTheChange()
    {
        var builds = new int[3];
        _subscriptions.Watch("a", 1, new ClaimsPrincipal(), new LiveInterest([LiveTopic.Entries]), Counting(builds, 0));
        _subscriptions.Watch("a", 2, new ClaimsPrincipal(), new LiveInterest([LiveTopic.Requests]), Counting(builds, 1));
        _subscriptions.Watch("b", 1, new ClaimsPrincipal(), new LiveInterest([LiveTopic.Entries, LiveTopic.Outputs], 7), Counting(builds, 2));
        await NextAsync(3);

        _subscriptions.Publish(new LiveChange(LiveTopic.Entries, 8));
        await NextAsync();
        _subscriptions.Publish(new LiveChange(LiveTopic.Outputs, 7));
        await NextAsync();

        CollectionAssert.AreEqual(new[] { 2, 1, 2 }, builds);
        Assert.IsFalse(_sent.Reader.TryRead(out _));
    }

    [TestMethod]
    public async Task Publish_WhileRefreshing_RefreshesOnceMoreAfterwards()
    {
        var builds = 0;
        var release = new TaskCompletionSource();
        _subscriptions.Watch("a", 1, new ClaimsPrincipal(), new LiveInterest([LiveTopic.Requests]), async (_, _) =>
        {
            if (Interlocked.Increment(ref builds) == 1)
                await release.Task;
            return builds;
        });

        for (var i = 0; i < 50; i++)
            _subscriptions.Publish(new LiveChange(LiveTopic.Requests));
        release.SetResult();

        await NextAsync(2);
        await Task.Delay(200);
        Assert.AreEqual(2, builds);
        Assert.IsFalse(_sent.Reader.TryRead(out _));
    }

    [TestMethod]
    public async Task Unwatch_AndDrop_StopTheUpdates()
    {
        _subscriptions.Watch("a", 1, new ClaimsPrincipal(), new LiveInterest([LiveTopic.Reports]), (_, _) => Task.FromResult<object?>(1));
        _subscriptions.Watch("a", 2, new ClaimsPrincipal(), new LiveInterest([LiveTopic.Reports]), (_, _) => Task.FromResult<object?>(2));
        _subscriptions.Watch("b", 1, new ClaimsPrincipal(), new LiveInterest([LiveTopic.Reports]), (_, _) => Task.FromResult<object?>(3));
        await NextAsync(3);

        _subscriptions.Unwatch("a", 1);
        _subscriptions.Drop("b");
        _subscriptions.Publish(new LiveChange(LiveTopic.Reports));

        var message = await NextAsync();
        CollectionAssert.AreEqual(new object?[] { 2, 2 }, message.Arguments);
        Assert.AreEqual(1, _subscriptions.Count);
        await Task.Delay(200);
        Assert.IsFalse(_sent.Reader.TryRead(out _));
    }

    [TestMethod]
    public async Task Watch_WhenTheViewIsGone_SaysItIsMissing()
    {
        _subscriptions.Watch("a", 3, new ClaimsPrincipal(), LiveInterest.Nothing, (_, _) => Task.FromResult<object?>(null));

        var message = await NextAsync();
        Assert.AreEqual(LiveSubscription.Missing, message.Method);
        CollectionAssert.AreEqual(new object?[] { 3 }, message.Arguments);
    }

    [TestMethod]
    public async Task Watch_WhenAccessIsDenied_SendsTheReason()
    {
        _subscriptions.Watch("a", 4, new ClaimsPrincipal(), LiveInterest.Nothing, (_, _) => throw new AccessDeniedException("Sign in."));

        var message = await NextAsync();
        Assert.AreEqual(LiveSubscription.Denied, message.Method);
        CollectionAssert.AreEqual(new object?[] { 4, "Sign in." }, message.Arguments);
    }

    private static Func<IViewBuilder, CancellationToken, Task<object?>> Counting(int[] builds, int index)
    {
        return (_, _) => Task.FromResult<object?>(Interlocked.Increment(ref builds[index]));
    }

    private async Task<(string ConnectionId, string Method, object?[] Arguments)> NextAsync(int count = 1)
    {
        using var timeout = new CancellationTokenSource(Timeout);
        (string, string, object?[]) message = default;
        for (var i = 0; i < count; i++)
            message = await _sent.Reader.ReadAsync(timeout.Token);
        return message;
    }
}
