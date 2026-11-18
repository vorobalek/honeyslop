using Microsoft.AspNetCore.WebUtilities;

namespace Honeyslop.Admin.Access;

public static class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        public string SignInUrl
        {
            get
            {
                var query = QueryHelpers.ParseQuery(request.QueryString.Value);
                query.Remove("handler");
                var returnUrl = request.PathBase.Add(request.Path).ToUriComponent() + QueryString.Create(query).ToUriComponent();
                return $"/signin?returnUrl={Uri.EscapeDataString(returnUrl)}";
            }
        }
    }
}
