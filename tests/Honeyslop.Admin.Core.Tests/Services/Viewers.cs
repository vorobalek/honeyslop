using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Data.Repositories;
using Moq;

namespace Honeyslop.Admin.Core.Tests.Services;

internal static class Viewers
{
    public static readonly ViewerDto Operator = new("operator-id", "operator", true);

    public static IViewerContext Context(ViewerDto viewer)
    {
        var context = new Mock<IViewerContext>();
        context.SetupGet(x => x.Viewer).Returns(viewer);
        return context.Object;
    }

    public static IAccessGuard Guard(ViewerDto viewer)
    {
        var guard = new Mock<IAccessGuard>();
        guard.SetupGet(x => x.Viewer).Returns(viewer);
        if (!viewer.CanOperate)
            guard.Setup(x => x.EnsureCanOperate()).Throws(new AccessDeniedException("Sign in to change anything."));
        return guard.Object;
    }

    public static IServerTimeRepository ServerTime(DateTimeOffset now)
    {
        var serverTime = new Mock<IServerTimeRepository>();
        serverTime.Setup(x => x.GetNowAsync(It.IsAny<CancellationToken>())).ReturnsAsync(now);
        return serverTime.Object;
    }
}
