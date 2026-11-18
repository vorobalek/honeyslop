using System.Security.Claims;
using Honeyslop.Admin.Access;
using Honeyslop.Admin.Access.Internals;
using Honeyslop.Admin.Core.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Honeyslop.Admin.Tests.Access;

[TestClass]
public sealed class AccessHandlerTests
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private static readonly ClaimsPrincipal SignedIn = new(new ClaimsIdentity([new Claim("sub", "admin")], "test"));

    [TestMethod]
    [DataRow("GET", false)]
    [DataRow("POST", false)]
    [DataRow("GET", true)]
    [DataRow("POST", true)]
    public async Task SignedIn_MayDoAnything(string method, bool publicView)
    {
        Assert.IsTrue(await AuthorizeAsync(SignedIn, method, publicView));
    }

    [TestMethod]
    [DataRow("GET")]
    [DataRow("HEAD")]
    public async Task Anonymous_MayReadOnlyInPublicView(string method)
    {
        Assert.IsTrue(await AuthorizeAsync(Anonymous, method, true));
        Assert.IsFalse(await AuthorizeAsync(Anonymous, method, false));
    }

    [TestMethod]
    [DataRow("POST")]
    [DataRow("PUT")]
    [DataRow("DELETE")]
    public async Task Anonymous_MayNotChangeAnythingEvenInPublicView(string method)
    {
        Assert.IsFalse(await AuthorizeAsync(Anonymous, method, true));
    }

    private static async Task<bool> AuthorizeAsync(ClaimsPrincipal user, string method, bool publicView)
    {
        var requirement = new AccessRequirement();
        var httpContext = new DefaultHttpContext { Request = { Method = method } };
        var context = new AuthorizationHandlerContext([requirement], user, httpContext);
        await new AccessHandler(new FixedPublicView(publicView)).HandleAsync(context);
        return context.HasSucceeded;
    }

    private sealed class FixedPublicView(bool enabled) : IPublicView
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(enabled);
        }

        public void Reset()
        {
        }
    }
}
