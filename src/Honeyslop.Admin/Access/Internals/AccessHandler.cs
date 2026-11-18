using Honeyslop.Admin.Core.Access;
using Microsoft.AspNetCore.Authorization;

namespace Honeyslop.Admin.Access.Internals;

internal sealed class AccessHandler(IPublicView publicView) : AuthorizationHandler<AccessRequirement, HttpContext>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccessRequirement requirement,
        HttpContext resource)
    {
        if (context.User.Identity?.IsAuthenticated == true ||
            IsReading(resource.Request.Method) && await publicView.IsEnabledAsync(resource.RequestAborted))
            context.Succeed(requirement);
    }

    private static bool IsReading(string method)
    {
        return HttpMethods.IsGet(method) || HttpMethods.IsHead(method);
    }
}
