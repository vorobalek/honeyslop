namespace Honeyslop.Admin.Middleware;

internal sealed class SecurityHeadersMiddleware : IMiddleware
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; frame-src 'self'; " +
        "frame-ancestors 'none'; form-action 'self'; base-uri 'none'; object-src 'none'";

    public Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        var headers = httpContext.Response.Headers;
        headers.ContentSecurityPolicy = ContentSecurityPolicy;
        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";
        headers.CacheControl = "no-cache, no-store";
        headers.Pragma = "no-cache";
        return next(httpContext);
    }
}
