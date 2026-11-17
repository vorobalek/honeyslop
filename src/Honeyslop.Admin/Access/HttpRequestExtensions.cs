using Microsoft.AspNetCore.Http.Extensions;

namespace Honeyslop.Admin.Access;

public static class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        public string SignInUrl => $"/signin?returnUrl={Uri.EscapeDataString(request.GetEncodedPathAndQuery())}";
    }
}
