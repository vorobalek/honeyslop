using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Access.Internals;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Exceptions;
using Moq;

namespace Honeyslop.Admin.Core.Tests.Services;

[TestClass]
public sealed class AccessGuardTests
{
    [TestMethod]
    public async Task EnsureCanReadAsync_AnonymousWithThePublicViewOff_IsDenied()
    {
        var guard = Guard(ViewerDto.Anonymous, false);

        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() => guard.EnsureCanReadAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task EnsureCanReadAsync_AnonymousWithThePublicViewOn_ReadsButCannotOperate()
    {
        var guard = Guard(ViewerDto.Anonymous, true);

        await guard.EnsureCanReadAsync(CancellationToken.None);

        Assert.ThrowsExactly<AccessDeniedException>(guard.EnsureCanOperate);
    }

    [TestMethod]
    public async Task Operator_ReadsAndOperatesWithThePublicViewOff()
    {
        var guard = Guard(Viewers.Operator, false);

        await guard.EnsureCanReadAsync(CancellationToken.None);
        guard.EnsureCanOperate();
    }

    private static AccessGuard Guard(ViewerDto viewer, bool publicView)
    {
        var view = new Mock<IPublicView>();
        view.Setup(x => x.IsEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(publicView);
        return new AccessGuard(Viewers.Context(viewer), view.Object);
    }
}