namespace Honeyslop.Trap.Responses;

internal static class NoiseWriter
{
    public static async Task WriteAsync(HttpContext httpContext, string path, CancellationToken cancellationToken)
    {
        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status200OK;
        response.Headers.ContentEncoding = "gzip";
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength = new FileInfo(path).Length;
        if (!HttpMethods.IsHead(httpContext.Request.Method))
            await response.SendFileAsync(path, cancellationToken);
    }
}