using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Access.Internals;

internal sealed class HttpViewerContext(IHttpContextAccessor httpContextAccessor) : IViewerContext
{
    public ViewerDto Viewer => httpContextAccessor.HttpContext?.User is { } user
        ? new ViewerDto(user.UserId, user.DisplayName, user.CanOperate)
        : ViewerDto.Anonymous;
}
