using System.Security.Claims;
using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Access.Internals;

internal sealed class ViewerContext(IHttpContextAccessor httpContextAccessor) : IViewerContext, IViewerScope
{
    private ClaimsPrincipal? _user;

    public ViewerDto Viewer => (_user ?? httpContextAccessor.HttpContext?.User) is { } user
        ? new ViewerDto(user.UserId, user.DisplayName, user.CanOperate)
        : ViewerDto.Anonymous;

    public void Use(ClaimsPrincipal user)
    {
        _user = user;
    }
}
