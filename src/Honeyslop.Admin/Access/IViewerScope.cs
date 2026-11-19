using System.Security.Claims;

namespace Honeyslop.Admin.Access;

public interface IViewerScope
{
    void Use(ClaimsPrincipal user);
}