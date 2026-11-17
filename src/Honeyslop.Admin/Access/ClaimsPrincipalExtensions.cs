using System.Security.Claims;
using Honeyslop.Admin.Configuration;

namespace Honeyslop.Admin.Access;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public bool CanOperate => !AppConfiguration.SignInEnabled || user.Identity?.IsAuthenticated == true;

        public string? DisplayName => user.FindFirst("preferred_username")?.Value ??
                                      user.FindFirst("name")?.Value ??
                                      user.FindFirst("sub")?.Value;
    }
}
