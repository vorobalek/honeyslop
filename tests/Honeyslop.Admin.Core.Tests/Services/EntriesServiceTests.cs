using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Core.Services.Internals;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Moq;

namespace Honeyslop.Admin.Core.Tests.Services;

[TestClass]
public sealed class EntriesServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 9, 0, 0, TimeSpan.Zero);

    private readonly Mock<IEntryRepository> _entries = new();
    private Entry[] _replacements = [];

    [TestInitialize]
    public void Initialize()
    {
        _entries
            .Setup(x => x.ReplaceAsync(It.IsAny<IReadOnlyCollection<Entry>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Entry>, DateTimeOffset, CancellationToken>((replacements, _, _) =>
            {
                _replacements = [.. replacements];
                var id = 100;
                foreach (var replacement in _replacements)
                    replacement.Id = ++id;
            })
            .Returns(Task.CompletedTask);
    }

    [TestMethod]
    public async Task RegenerateAsync_QueuesACopyOfEveryActiveEntryWithTheGuidance()
    {
        Active(Ready(1, "/SDK/webLanguage"), Ready(2, "/.aws/credentials"));

        var change = await Service(Viewers.Operator).RegenerateAsync([1, 2, 3], "  Answer as a Hikvision camera  ", CancellationToken.None);

        Assert.IsTrue(change.Changed);
        CollectionAssert.AreEqual(new long[] { 101, 102 }, change.Ids);
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
    public async Task RegenerateAsync_WhenEveryEntryHasExpired_ChangesNothing()
    {
        Active();

        var change = await Service(Viewers.Operator).RegenerateAsync([1], null, CancellationToken.None);

        Assert.IsFalse(change.Changed);
        CollectionAssert.AreEqual(new[] { "Entries" }, change.Errors.Keys.ToArray());
        _entries.Verify(x => x.ReplaceAsync(It.IsAny<IReadOnlyCollection<Entry>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ReplaceAsync_InvalidResponse_IsRefusedAndNothingChanges()
    {
        Active(Ready(1, "/"));

        var change = await Service(Viewers.Operator).ReplaceAsync(
            [1],
            new EntryResponseDto(99, [new ResponseHeader { Name = "Content-Length", Value = "5" }], "hello"),
            CancellationToken.None);

        Assert.IsFalse(change.Changed);
        CollectionAssert.AreEquivalent(new[] { "Status", "Headers" }, change.Errors.Keys.ToArray());
        StringAssert.Contains(change.Errors["Headers"], "Content-Length is set by the server");
        _entries.Verify(x => x.ReplaceAsync(It.IsAny<IReadOnlyCollection<Entry>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ReplaceAsync_StoresTheResponseAsReadyWithADefaultContentType()
    {
        Active(Ready(1, "/.git-credentials"));

        var change = await Service(Viewers.Operator).ReplaceAsync(
            [1],
            new EntryResponseDto(200, [new ResponseHeader { Name = "Server", Value = "nginx/1.18.0" }], "https://deploy:token@github.com"),
            CancellationToken.None);

        Assert.IsTrue(change.Changed);
        var replacement = _replacements.Single();
        Assert.AreEqual(EntryStatus.Ready, replacement.Status);
        Assert.AreEqual(200, replacement.ResponseStatus);
        Assert.AreEqual("https://deploy:token@github.com", replacement.ResponseBody);
        Assert.AreEqual(Now, replacement.GeneratedAt);
        CollectionAssert.AreEqual(
            new[] { "Server: nginx/1.18.0", "Content-Type: text/html; charset=utf-8" },
            replacement.ResponseHeaders.Select(x => $"{x.Name}: {x.Value}").ToArray());
    }

    [TestMethod]
    public async Task RegenerateAsync_Anonymous_IsDenied()
    {
        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() =>
            Service(ViewerDto.Anonymous).RegenerateAsync([1], null, CancellationToken.None));
    }

    private void Active(params Entry[] entries)
    {
        _entries
            .Setup(x => x.GetActiveAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
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
            Viewers.ServerTime(Now));
    }
}
