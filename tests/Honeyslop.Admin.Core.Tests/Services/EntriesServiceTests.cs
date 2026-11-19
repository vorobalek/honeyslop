using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Core.Services.Internals;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Data.Requests;
using Moq;

namespace Honeyslop.Admin.Core.Tests.Services;

[TestClass]
public sealed class EntriesServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 9, 0, 0, TimeSpan.Zero);

    private readonly Mock<IEntryRepository> _entries = new();
    private readonly Mock<ISettingsRepository> _settings = new();
    private Entry[] _replacements = [];

    [TestInitialize]
    public void Initialize()
    {
        _entries
            .Setup(x => x.TryReplaceAsync(It.IsAny<IReadOnlyCollection<Entry>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Entry>, DateTimeOffset, CancellationToken>((replacements, _, _) =>
            {
                _replacements = [.. replacements];
                var id = 100;
                foreach (var replacement in _replacements)
                    replacement.Id = ++id;
            })
            .ReturnsAsync(true);
    }

    [TestMethod]
    public async Task RegenerateAsync_QueuesACopyOfEveryActiveEntryWithTheGuidance()
    {
        Active(Ready(1, "/SDK/webLanguage"), Ready(2, "/.aws/credentials"));

        var change = await Service(Viewers.Operator).RegenerateAsync([1, 2, 3], "  Answer as a Hikvision camera  ", CancellationToken.None);

        Assert.IsTrue(change.Changed);
        CollectionAssert.AreEqual(new long[]
        {
            101, 102
        }, change.Ids);
        Assert.HasCount(2, _replacements);
        foreach (var replacement in _replacements)
        {
            Assert.AreEqual(EntryStatus.Pending, replacement.Status);
            Assert.AreEqual("Answer as a Hikvision camera", replacement.Guidance);
            Assert.AreEqual("operator", replacement.ChangedBy);
            Assert.AreEqual(Now, replacement.CreatedAt);
            Assert.IsNull(replacement.ResponseStatus);
        }

        Assert.AreEqual("/SDK/webLanguage", _replacements[0].Target);
        Assert.AreEqual("User-Agent: Go-http-client/1.1", _replacements[0].RequestHeaders);
    }

    [TestMethod]
    public async Task RegenerateAsync_ExpiredEntriesOfOnePath_GetOneReplacementFromTheLatest()
    {
        var older = Ready(1, "/SDK/webLanguage");
        var latest = Ready(2, "/SDK/webLanguage");
        older.Status = latest.Status = EntryStatus.Expired;
        latest.KeyHash = older.KeyHash;
        latest.RequestHeaders = "User-Agent: latest";
        Active(older, latest);

        var change = await Service(Viewers.Operator).RegenerateAsync([1, 2], null, CancellationToken.None);

        Assert.IsTrue(change.Changed);
        Assert.HasCount(1, _replacements);
        Assert.AreEqual("User-Agent: latest", _replacements[0].RequestHeaders);
        Assert.AreEqual(EntryStatus.Pending, _replacements[0].Status);
    }

    [TestMethod]
    public async Task RegenerateAsync_WhenAnotherChangeGetsThereFirst_IsRefused()
    {
        Active(Ready(1, "/SDK/webLanguage"));
        _entries
            .Setup(x => x.TryReplaceAsync(It.IsAny<IReadOnlyCollection<Entry>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var change = await Service(Viewers.Operator).RegenerateAsync([1], null, CancellationToken.None);

        Assert.IsFalse(change.Changed);
        CollectionAssert.AreEqual(new[]
        {
            "Entries"
        }, change.Errors.Keys.ToArray());
        Assert.IsEmpty(change.Ids);
    }

    [TestMethod]
    public async Task RegenerateAsync_WhenEveryEntryHasExpired_ChangesNothing()
    {
        Active();

        var change = await Service(Viewers.Operator).RegenerateAsync([1], null, CancellationToken.None);

        Assert.IsFalse(change.Changed);
        CollectionAssert.AreEqual(new[]
        {
            "Entries"
        }, change.Errors.Keys.ToArray());
        _entries.Verify(x => x.TryReplaceAsync(It.IsAny<IReadOnlyCollection<Entry>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ReplaceAsync_InvalidResponse_IsRefusedAndNothingChanges()
    {
        Active(Ready(1, "/"));

        var change = await Service(Viewers.Operator).ReplaceAsync(
            [1],
            new EntryResponseDto(99, [
                new ResponseHeader
                {
                    Name = "Content-Length",
                    Value = "5"
                }
            ], "hello"),
            CancellationToken.None);

        Assert.IsFalse(change.Changed);
        CollectionAssert.AreEquivalent(new[]
        {
            "Status", "Headers"
        }, change.Errors.Keys.ToArray());
        StringAssert.Contains(change.Errors["Headers"], "Content-Length is set by the server");
        _entries.Verify(x => x.TryReplaceAsync(It.IsAny<IReadOnlyCollection<Entry>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ReplaceAsync_StoresTheResponseAsReadyWithADefaultContentType()
    {
        Active(Ready(1, "/.git-credentials"));

        var change = await Service(Viewers.Operator).ReplaceAsync(
            [1],
            new EntryResponseDto(200, [
                new ResponseHeader
                {
                    Name = "Server",
                    Value = "nginx/1.18.0"
                }
            ], "https://deploy:token@github.com"),
            CancellationToken.None);

        Assert.IsTrue(change.Changed);
        var replacement = _replacements.Single();
        Assert.AreEqual(EntryStatus.Ready, replacement.Status);
        Assert.AreEqual(200, replacement.ResponseStatus);
        Assert.AreEqual("https://deploy:token@github.com", replacement.ResponseBody);
        Assert.AreEqual(Now, replacement.GeneratedAt);
        CollectionAssert.AreEqual(
            new[]
            {
                "Server: nginx/1.18.0", "Content-Type: text/html; charset=utf-8"
            },
            replacement.ResponseHeaders.Select(x => $"{x.Name}: {x.Value}").ToArray());
    }

    [TestMethod]
    public async Task CreateAsync_QueuesTheRequestUnderTheTrapsKey()
    {
        var added = Adding(true);

        var change = await Service(Viewers.Operator).CreateAsync(
            new EntryRequestDto("head", " Camera.Example ", "/SDK/webLanguage", "User-Agent: Go-http-client/1.1\r\n\n  Accept: */*  ", null),
            "Answer as a Hikvision camera",
            CancellationToken.None);

        Assert.IsTrue(change.Changed);
        var entry = added();
        CollectionAssert.AreEqual(new[]
        {
            entry.Id
        }, change.Ids);
        Assert.AreEqual("GET", entry.Method);
        Assert.AreEqual("camera.example", entry.Host);
        CollectionAssert.AreEqual(EntryKey.Create("GET", "camera.example", "/SDK/webLanguage").Hash, entry.KeyHash);
        Assert.AreEqual(EntryStatus.Pending, entry.Status);
        Assert.AreEqual("Answer as a Hikvision camera", entry.Guidance);
        Assert.AreEqual("Go-http-client/1.1", entry.UserAgent);
        Assert.AreEqual("User-Agent: Go-http-client/1.1\nAccept: */*\n", entry.RequestHeaders);
        Assert.AreEqual("operator", entry.ChangedBy);
    }

    [TestMethod]
    public async Task CreateAsync_WithAResponse_StoresItReady()
    {
        var added = Adding(true);

        var change = await Service(Viewers.Operator).CreateWithResponseAsync(
            new EntryRequestDto("GET", "camera.example", "/SDK/webLanguage", null, null),
            new EntryResponseDto(200, [], "<Language><type>English</type></Language>"),
            CancellationToken.None);

        Assert.IsTrue(change.Changed);
        var entry = added();
        Assert.AreEqual(EntryStatus.Ready, entry.Status);
        Assert.AreEqual("<Language><type>English</type></Language>", entry.ResponseBody);
        Assert.AreEqual("Content-Type", entry.ResponseHeaders.Single().Name);
        Assert.IsNull(entry.RequestHeaders);
    }

    [TestMethod]
    public async Task CreateAsync_InvalidRequest_SaysWhatIsWrongWithEachField()
    {
        var change = await Service(Viewers.Operator).CreateAsync(
            new EntryRequestDto("TRACE", "http://camera.example:80", "/logo.png", "not a header", null),
            null,
            CancellationToken.None);

        Assert.IsFalse(change.Changed);
        CollectionAssert.AreEquivalent(new[]
        {
            "Method", "Host", "RequestHeaders"
        }, change.Errors.Keys.ToArray());
        _entries.Verify(x => x.TryAddAsync(It.IsAny<Entry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    [DataRow("/logo.png")]
    [DataRow("SDK/webLanguage")]
    [DataRow("/with space")]
    [DataRow("")]
    public void CreateAsync_TargetTheTrapWouldNotGenerateFor_IsRefused(string target)
    {
        var change = Service(Viewers.Operator)
            .CreateAsync(new EntryRequestDto("GET", "camera.example", target, null, null), null, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        CollectionAssert.AreEqual(new[]
        {
            "Target"
        }, change.Errors.Keys.ToArray());
    }

    [TestMethod]
    public async Task CreateAsync_WhenTheRequestHasAnEntry_PointsToIt()
    {
        Adding(false);
        _entries
            .Setup(x => x.GetByKeyHashOrDefaultAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ready(42, "/SDK/webLanguage"));

        var change = await Service(Viewers.Operator).CreateAsync(
            new EntryRequestDto("GET", "camera.example", "/SDK/webLanguage", null, null),
            null,
            CancellationToken.None);

        Assert.IsFalse(change.Changed);
        StringAssert.Contains(change.Errors["Entries"], "42");
        CollectionAssert.AreEqual(new long[]
        {
            42
        }, change.Ids);
    }

    [TestMethod]
    public async Task CreateAsync_Anonymous_IsDenied()
    {
        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() =>
            Service(ViewerDto.Anonymous).CreateAsync(new EntryRequestDto("GET", "camera.example", "/", null, null), null, CancellationToken.None));
    }

    [TestMethod]
    public async Task RegenerateAsync_KeepsTheDelayOfTheEntryItReplaces()
    {
        var current = Ready(1, "/SDK/webLanguage");
        current.ResponseDelayMilliseconds = 30_000;
        Active(current);

        await Service(Viewers.Operator).RegenerateAsync([1], null, CancellationToken.None);

        Assert.AreEqual(30_000, _replacements.Single().ResponseDelayMilliseconds);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(600_001)]
    public async Task SetResponseDelayAsync_OutOfRange_IsRefused(int milliseconds)
    {
        var change = await Service(Viewers.Operator).SetResponseDelayAsync([1], milliseconds, CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            nameof(Entry.ResponseDelayMilliseconds)
        }, change.Errors.Keys.ToArray());
        _entries.Verify(x => x.SetResponseDelayAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<int?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SetResponseDelayAsync_ReturnsTheEntriesItChanged()
    {
        _entries
            .Setup(x => x.SetResponseDelayAsync(It.IsAny<IReadOnlyCollection<long>>(), 15, Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync([2]);

        var change = await Service(Viewers.Operator).SetResponseDelayAsync([1, 2], 15, CancellationToken.None);

        Assert.IsTrue(change.Changed);
        CollectionAssert.AreEqual(new long[]
        {
            2
        }, change.Ids);
    }

    [TestMethod]
    public async Task SetResponseDelayAsync_WhenNothingIsServed_IsRefused()
    {
        _entries
            .Setup(x => x.SetResponseDelayAsync(It.IsAny<IReadOnlyCollection<long>>(), null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var change = await Service(Viewers.Operator).SetResponseDelayAsync([1], null, CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            "Entries"
        }, change.Errors.Keys.ToArray());
    }

    [TestMethod]
    public async Task RegenerateAsync_Anonymous_IsDenied()
    {
        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() =>
            Service(ViewerDto.Anonymous).RegenerateAsync([1], null, CancellationToken.None));
    }

    private Func<Entry> Adding(bool added)
    {
        Entry? captured = null;
        _entries
            .Setup(x => x.TryAddAsync(It.IsAny<Entry>(), It.IsAny<CancellationToken>()))
            .Callback<Entry, CancellationToken>((entry, _) =>
            {
                entry.Id = 77;
                captured = entry;
            })
            .ReturnsAsync(added);
        return () => captured ?? throw new AssertFailedException("Nothing was added.");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetAsync_ShowsWhoChangedTheEntryAndTheCommentOnlyToOperators(bool signedIn)
    {
        var entry = Ready(5, "/index.php");
        entry.ChangedBy = "operator";
        entry.Guidance = "return rendered HTML";
        _entries.Setup(x => x.GetByIdOrDefaultAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        _settings.Setup(x => x.GetAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Settings());

        var details = await Service(signedIn ? Viewers.Operator : ViewerDto.Anonymous).GetAsync(5, CancellationToken.None);

        Assert.AreEqual(signedIn ? "operator" : null, details!.Entry.ChangedBy);
        Assert.AreEqual(signedIn ? "return rendered HTML" : null, details.Entry.Guidance);
    }

    private void Active(params Entry[] entries)
    {
        _entries
            .Setup(x => x.GetChangeableAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);
    }

    private static Entry Ready(long id, string target)
    {
        return new Entry
        {
            Id = id,
            KeyHash = [(byte)id],
            Method = "GET",
            Host = "135.181.59.187",
            Target = target,
            Status = EntryStatus.Ready,
            RequestHeaders = "User-Agent: Go-http-client/1.1",
            ResponseStatus = 404,
            ResponseBody = "404 Not Found",
            Hits = 7
        };
    }

    private IEntriesService Service(ViewerDto viewer)
    {
        return new EntriesService(
            _entries.Object,
            Mock.Of<IGenerationOutputStore>(),
            Mock.Of<ICountryLookup>(),
            Viewers.Guard(viewer),
            Viewers.ServerTime(Now),
            _settings.Object,
            Microsoft.Extensions.Options.Options.Create(new RequestRules()),
            Microsoft.Extensions.Options.Options.Create(new ResponseDelayOptions()));
    }
}