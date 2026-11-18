using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Exceptions;

namespace Honeyslop.Admin.Core.Access.Internals;

internal sealed class AccessGuard(IViewerContext viewerContext, IPublicView publicView) : IAccessGuard
{
    public ViewerDto Viewer => viewerContext.Viewer;

    public async Task EnsureCanReadAsync(CancellationToken cancellationToken)
    {
        if (!Viewer.CanOperate && !await publicView.IsEnabledAsync(cancellationToken))
            throw new AccessDeniedException("Sign in to see honeyslop: its public view is off.");
    }

    public void EnsureCanOperate()
    {
        if (!Viewer.CanOperate)
            throw new AccessDeniedException("Sign in to change anything.");
    }
}
