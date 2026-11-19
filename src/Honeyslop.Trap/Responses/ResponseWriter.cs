namespace Honeyslop.Trap.Responses;

internal static class ResponseWriter
{
    private const string ContentSecurityPolicy =
        "sandbox; default-src 'none'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; font-src 'self' data:";

    public static async Task WriteAsync(
        HttpContext httpContext,
        CachedResponse response,
        CancellationToken cancellationToken)
    {
        var httpResponse = httpContext.Response;
        httpResponse.StatusCode = response.Status;
        foreach (var header in response.Headers)
            httpResponse.Headers.Append(header.Name, header.Value);
        httpResponse.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
        httpResponse.Headers.XContentTypeOptions = "nosniff";

        if (response.Status is StatusCodes.Status204NoContent or StatusCodes.Status304NotModified)
            return;

        httpResponse.ContentLength = response.Body.Length;
        if (!HttpMethods.IsHead(httpContext.Request.Method) && response.Body.Length > 0)
            await httpResponse.Body.WriteAsync(response.Body, cancellationToken);
    }
}