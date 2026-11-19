using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Core.Services.Internals;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Moq;

namespace Honeyslop.Admin.Core.Tests.Services;

[TestClass]
public sealed class BlockedContactsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IBlockedAbuseContactRepository> _repository = new();

    [TestMethod]
    public async Task BlockAsync_AddsEveryContactOnceInLowerCaseWithWhoAndWhy()
    {
        BlockedAbuseContact[] added = [];
        _repository
            .Setup(x => x.AddAsync(It.IsAny<IReadOnlyCollection<BlockedAbuseContact>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<BlockedAbuseContact>, CancellationToken>((contacts, _) => added = [.. contacts])
            .ReturnsAsync(2);

        var change = await Service(Viewers.Operator).BlockAsync([" Abuse@Bouncing.example ", "@bouncing.example", "abuse@bouncing.example"], " bounces ", CancellationToken.None);

        Assert.IsTrue(change.Done);
        Assert.AreEqual(2, change.Changed);
        CollectionAssert.AreEqual(new[]
        {
            "abuse@bouncing.example", "@bouncing.example"
        }, added.Select(x => x.Contact).ToArray());
        Assert.IsTrue(added.All(x => x is { Note: "bounces", CreatedBy: "operator" } && x.CreatedAt == Now));
    }

    [TestMethod]
    [DataRow("bouncing.example")]
    [DataRow("")]
    public async Task BlockAsync_SomethingThatIsNoContact_IsRefused(string contact)
    {
        var change = await Service(Viewers.Operator).BlockAsync([contact], null, CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            "Contacts"
        }, change.Errors.Keys.ToArray());
        _repository.Verify(x => x.AddAsync(It.IsAny<IReadOnlyCollection<BlockedAbuseContact>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task BlockAsync_MoreThanAThousand_IsRefused()
    {
        var change = await Service(Viewers.Operator).BlockAsync(
            [.. Enumerable.Range(0, IBlockedContactsService.MaxPerChange + 1).Select(i => $"abuse{i}@bouncing.example")],
            null,
            CancellationToken.None);

        Assert.IsFalse(change.Done);
    }

    [TestMethod]
    public async Task GetPageAsync_Anonymous_DoesNotSeeWhoBlocked()
    {
        _repository
            .Setup(x => x.GetPageAsync(null, 0, IBlockedContactsService.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new BlockedAbuseContact
                {
                    Contact = "@bouncing.example",
                    CreatedBy = "operator",
                    CreatedAt = Now
                }
            ]);
        _repository.Setup(x => x.CountAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var anonymous = await Service(ViewerDto.Anonymous).GetPageAsync(new BlockedContactsQuery(), CancellationToken.None);
        var signedIn = await Service(Viewers.Operator).GetPageAsync(new BlockedContactsQuery(), CancellationToken.None);

        Assert.IsNull(anonymous.Contacts.Items.Single().CreatedBy);
        Assert.AreEqual("operator", signedIn.Contacts.Items.Single().CreatedBy);
    }

    [TestMethod]
    public async Task Changes_Anonymous_AreDenied()
    {
        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() => Service(ViewerDto.Anonymous).BlockAsync(["@bouncing.example"], null, CancellationToken.None));
        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() => Service(ViewerDto.Anonymous).UnblockAsync(["@bouncing.example"], CancellationToken.None));
    }

    private BlockedContactsService Service(ViewerDto viewer)
    {
        return new BlockedContactsService(_repository.Object, Viewers.Guard(viewer), Viewers.ServerTime(Now));
    }
}