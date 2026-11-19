namespace Honeyslop.Trap.Responses;

internal static class ResponseWriter
{
    private const string ContentSecurityPolicy =
        "sandbox; default-src 'none'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; font-src 'self' data:";

    private static readonly TimeSpan LongestPause = TimeSpan.FromSeconds(1);

    public static async Task TrickleAsync(
        HttpContext httpContext,
        CachedResponse response,
        TimeSpan remaining,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var body = HasBody(httpContext, response) ? response.Body : [];
        var pauses = (int)Math.Min(body.Length, Math.Ceiling(remaining / LongestPause));
        if (pauses <= 0)
        {
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining, timeProvider, cancellationToken);
            await WriteAsync(httpContext, response, cancellationToken);
            return;
        }

        var pause = remaining / pauses;
        if (pause > LongestPause)
        {
            await Task.Delay(remaining - LongestPause * pauses, timeProvider, cancellationToken);
            pause = LongestPause;
        }

        WriteHead(httpContext, response);
        await httpContext.Response.StartAsync(cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
        var started = timeProvider.GetTimestamp();
        for (var part = 0; part < pauses; part++)
        {
            var wait = pause * (part + 1) - timeProvider.GetElapsedTime(started);
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, timeProvider, cancellationToken);
            var from = body.Length * part / pauses;
            var to = body.Length * (part + 1) / pauses;
            await httpContext.Response.Body.WriteAsync(body.AsMemory(from, to - from), cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
        }
    }

    public static async Task WriteAsync(
        HttpContext httpContext,
        CachedResponse response,
        CancellationToken cancellationToken)
    {
        WriteHead(httpContext, response);
        if (HasBody(httpContext, response))
            await httpContext.Response.Body.WriteAsync(response.Body, cancellationToken);
    }

    private static bool HasBody(HttpContext httpContext, CachedResponse response)
    {
        return response.Status is not (StatusCodes.Status204NoContent or StatusCodes.Status304NotModified) &&
               !HttpMethods.IsHead(httpContext.Request.Method) &&
               response.Body.Length > 0;
    }

    private static void WriteHead(HttpContext httpContext, CachedResponse response)
    {
        var httpResponse = httpContext.Response;
        httpResponse.StatusCode = response.Status;
        foreach (var header in response.Headers)
            httpResponse.Headers.Append(header.Name, header.Value);
        httpResponse.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
        httpResponse.Headers.XContentTypeOptions = "nosniff";

        if (response.Status is not (StatusCodes.Status204NoContent or StatusCodes.Status304NotModified))
            httpResponse.ContentLength = response.Body.Length;
    }
}