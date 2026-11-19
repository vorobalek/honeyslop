using Honeyslop.Admin.Access;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Reports.Contracts;

namespace Honeyslop.Admin.Views;

internal static class ContentEndpoints
{
    private const string PreviewContentSecurityPolicy =
        "sandbox; default-src 'none'; img-src data:; style-src 'unsafe-inline'; font-src data:; frame-ancestors 'self'";

    public static async Task<IResult> ArchiveAsync(string ip, HttpContext httpContext, IClientsService clientsService)
    {
        if (!httpContext.User.CanOperate)
            return Results.Challenge();
        var archive = await clientsService.GetArchiveAsync(ip, httpContext.RequestAborted);
        return archive is null
            ? Results.NotFound()
            : Results.File(archive.Content, "application/zip", RequestLogArchive.FileName(ip));
    }

    public static async Task<IResult> PreviewAsync(long id, HttpContext httpContext, IEntriesService entriesService)
    {
        var body = await entriesService.GetPreviewAsync(id, httpContext.RequestAborted);
        if (body is null)
            return Results.NotFound();
        httpContext.Response.Headers.ContentSecurityPolicy = PreviewContentSecurityPolicy;
        return Results.Content(body, "text/html; charset=utf-8");
    }
}
