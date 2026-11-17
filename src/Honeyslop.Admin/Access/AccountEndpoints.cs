using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Honeyslop.Admin.Access;

internal static class AccountEndpoints
{
    public static IResult SignIn(string? returnUrl)
    {
        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = LocalUrlOrRoot(returnUrl) },
            [OpenIdConnectDefaults.AuthenticationScheme]);
    }

    public static async Task<IResult> SignOutAsync(HttpContext httpContext, IAntiforgery antiforgery)
    {
        if (!await antiforgery.IsRequestValidAsync(httpContext))
            return Results.BadRequest();

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect("/");
    }

    public static string LocalUrlOrRoot(string? url)
    {
        return url is ['/'] or ['/', not ('/' or '\\'), ..] ? url : "/";
    }
}
