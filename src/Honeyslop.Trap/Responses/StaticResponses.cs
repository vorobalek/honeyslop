using System.Text;
using Honeyslop.Data.Entities;
using Honeyslop.Trap.Requests;

namespace Honeyslop.Trap.Responses;

public static class StaticResponses
{
    public static readonly CachedResponse NotFound = Create(StatusCodes.Status404NotFound, "404 Not Found");
    public static readonly CachedResponse MethodNotAllowed = Create(StatusCodes.Status405MethodNotAllowed, "405 Not Allowed");
    public static readonly CachedResponse UriTooLong = Create(StatusCodes.Status414UriTooLong, "414 Request-URI Too Large");
    public static readonly CachedResponse BadRequest = Create(StatusCodes.Status400BadRequest, "400 Bad Request");

    public static CachedResponse For(ClassificationKind kind)
    {
        return kind switch
        {
            ClassificationKind.MethodNotAllowed => MethodNotAllowed,
            ClassificationKind.UriTooLong => UriTooLong,
            ClassificationKind.BadRequest => BadRequest,
            _ => NotFound
        };
    }

    private static CachedResponse Create(int status, string title)
    {
        var body =
            $"<html>\r\n<head><title>{title}</title></head>\r\n<body>\r\n" +
            $"<center><h1>{title}</h1></center>\r\n<hr><center>nginx</center>\r\n</body>\r\n</html>\r\n";
        return new CachedResponse(
            0,
            status,
            [
                new ResponseHeader
                {
                    Name = "Server",
                    Value = "nginx"
                },
                new ResponseHeader
                {
                    Name = "Content-Type",
                    Value = "text/html"
                }
            ],
            Encoding.ASCII.GetBytes(body));
    }
}
