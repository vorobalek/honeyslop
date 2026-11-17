using Honeyslop.Admin.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Honeyslop.Admin.Access;

public static class WebHostBuilderExtensions
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);

    extension(IWebHostBuilder builder)
    {
        public IWebHostBuilder AddAccess()
        {
            return builder.ConfigureServices(services =>
            {
                services
                    .AddSingleton<IPublicView, PublicView>()
                    .AddSingleton<IAuthorizationHandler, AccessHandler>()
                    .Configure<ForwardedHeadersOptions>(options =>
                    {
                        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
                        options.KnownIPNetworks.Clear();
                        options.KnownProxies.Clear();
                    });

                if (!AppConfiguration.SignInEnabled)
                {
                    services.AddAuthentication();
                    services.AddAuthorization();
                    return;
                }

                var authority = AppConfiguration.OidcAuthority!;
                var clientSecret = AppConfiguration.OidcClientSecret ??
                                   throw new ArgumentNullException("OIDC_CLIENT_SECRET", "Environment variable 'OIDC_CLIENT_SECRET' is not defined.");
                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    })
                    .AddCookie(options =>
                    {
                        options.Cookie.Name = "honeyslop";
                        options.ExpireTimeSpan = SessionLifetime;
                        options.SlidingExpiration = true;
                    })
                    .AddOpenIdConnect(options =>
                    {
                        options.Authority = authority;
                        options.RequireHttpsMetadata = authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                        options.ClientId = AppConfiguration.OidcClientId;
                        options.ClientSecret = clientSecret;
                        options.ResponseType = OpenIdConnectResponseType.Code;
                        options.ResponseMode = OpenIdConnectResponseMode.Query;
                        options.MapInboundClaims = false;
                        options.GetClaimsFromUserInfoEndpoint = true;
                        options.ClaimActions.MapUniqueJsonKey("preferred_username", "preferred_username");
                        options.TokenValidationParameters.NameClaimType = "preferred_username";
                        options.Events.OnRemoteFailure = context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return context.Response.WriteAsync($"Sign-in failed: {context.Failure?.Message}", context.HttpContext.RequestAborted);
                        };
                    });
                services
                    .AddAuthorizationBuilder()
                    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                        .AddRequirements(new AccessRequirement())
                        .Build());
            });
        }
    }
}
