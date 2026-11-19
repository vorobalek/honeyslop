using Honeyslop.Admin.Configuration;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Mcp.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;

namespace Honeyslop.Admin.Mcp;

public static class WebHostBuilderExtensions
{
    public const string Path = "/mcp";
    private const string Policy = "mcp";

    extension(IWebHostBuilder builder)
    {
        public IWebHostBuilder AddMcp()
        {
            return builder.ConfigureServices(services =>
            {
                if (!AppConfiguration.McpEnabled)
                    return;

                var authority = AppConfiguration.OidcAuthority!;
                var resource = AppConfiguration.McpResource!;
                services
                    .AddAuthentication()
                    .AddJwtBearer(options =>
                    {
                        options.Authority = authority;
                        options.RequireHttpsMetadata = authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters.ValidAudience = resource;
                        options.TokenValidationParameters.NameClaimType = "preferred_username";
                        options.ForwardChallenge = McpAuthenticationDefaults.AuthenticationScheme;
                    })
                    .AddMcp(options => options.ResourceMetadata = new ProtectedResourceMetadata
                    {
                        Resource = resource,
                        ResourceName = "honeyslop",
                        AuthorizationServers =
                        {
                            authority
                        },
                        ScopesSupported = ["openid", "profile", "offline_access"]
                    });
                services
                    .AddAuthorizationBuilder()
                    .AddPolicy(Policy, policy => policy
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser());
                services
                    .AddMcpServer(options => options.ServerInfo = new Implementation
                    {
                        Name = "honeyslop",
                        Version = "1.0"
                    })
                    .WithHttpTransport(options => options.Stateless = true)
                    .WithTools<Entries>()
                    .WithTools<Requests>()
                    .WithTools<Clients>()
                    .WithTools<Tools.Reports>()
                    .WithTools<Generators>()
                    .WithTools<Settings>()
                    .WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, cancellationToken) =>
                    {
                        try
                        {
                            return await next(context, cancellationToken);
                        }
                        catch (Exception exception) when (exception is AccessDeniedException or McpException)
                        {
                            return new CallToolResult
                            {
                                IsError = true,
                                Content =
                                [
                                    new TextContentBlock
                                    {
                                        Text = exception.Message
                                    }
                                ]
                            };
                        }
                    }));
            });
        }
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapHoneyslopMcp()
        {
            if (AppConfiguration.McpEnabled)
                endpoints.MapMcp(Path).RequireAuthorization(Policy);
            return endpoints;
        }
    }
}