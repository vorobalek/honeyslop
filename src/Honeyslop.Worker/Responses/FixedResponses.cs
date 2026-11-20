using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Honeyslop.Data.Entities;
using Honeyslop.Worker.Llm;

namespace Honeyslop.Worker.Responses;

public static partial class FixedResponses
{
    private static readonly string IisNotFound =
        """
        <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
        <html xmlns="http://www.w3.org/1999/xhtml">
        <head>
        <meta http-equiv="Content-Type" content="text/html; charset=iso-8859-1"/>
        <title>404 - File or directory not found.</title>
        <style type="text/css">
        <!--
        body{margin:0;font-size:.7em;font-family:Verdana, Arial, Helvetica, sans-serif;background:#EEEEEE;}
        fieldset{padding:0 15px 10px 15px;}
        h1{font-size:2.4em;margin:0;color:#FFF;}
        h2{font-size:1.7em;margin:0;color:#CC0000;}
        h3{font-size:1.2em;margin:10px 0 0 0;color:#000000;}
        #header{width:96%;margin:0 0 0 0;padding:6px 2% 6px 2%;font-family:"trebuchet MS", Verdana, sans-serif;color:#FFF;
        background-color:#555555;}
        #content{margin:0 0 0 2%;position:relative;}
        .content-container{background:#FFF;width:96%;margin-top:8px;padding:10px;position:relative;}
        -->
        </style>
        </head>
        <body>
        <div id="header"><h1>Server Error</h1></div>
        <div id="content">
         <div class="content-container"><fieldset>
          <h2>404 - File or directory not found.</h2>
          <h3>The resource you are looking for might have been removed, had its name changed, or is temporarily unavailable.</h3>
         </fieldset></div>
        </div>
        </body>
        </html>

        """.ReplaceLineEndings("\r\n");

    private const string LighttpdNotFound =
        """
        <?xml version="1.0" encoding="iso-8859-1"?>
        <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
                 "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
        <html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
         <head>
          <title>404 - Not Found</title>
         </head>
         <body>
          <h1>404 - Not Found</h1>
         </body>
        </html>

        """;

    public static GeneratedResponse? Respond(string host, string target, RequestProfile profile, bool allowRefusals)
    {
        if (profile.Kind != ResponseKind.File)
            return null;

        var path = RequestProfiler.LocalPath(target);
        if (GitRefRegex().Match(path) is { Success: true } git)
            return new GeneratedResponse(
                StatusCodes.Status200OK,
                Headers(profile.Server, ("Content-Type", "application/octet-stream"), ("Accept-Ranges", "bytes")),
                git.Groups["file"].Value == "HEAD"
                    ? $"ref: refs/heads/{(Hash(host.ToLowerInvariant())[0] < 0x80 ? "master" : "main")}\n"
                    : Convert.ToHexStringLower(Hash(host.ToLowerInvariant() + path)) + "\n");

        return allowRefusals && profile.Refusable && RequestProfiler.IsBinaryFile(target)
            ? NotFound(profile.Server, host, target)
            : null;
    }

    public static GeneratedResponse? NotFound(string server, string host, string target)
    {
        if (server.StartsWith("nginx/", StringComparison.Ordinal) || server.StartsWith("openresty/", StringComparison.Ordinal))
            return Page(server, "text/html", $"<html>\r\n<head><title>404 Not Found</title></head>\r\n<body>\r\n<center><h1>404 Not Found</h1></center>\r\n<hr><center>{server}</center>\r\n</body>\r\n</html>\r\n");

        if (ApacheRegex().Match(server) is { Success: true } apache)
        {
            var minor = int.Parse(apache.Groups["minor"].Value);
            var patch = int.Parse(apache.Groups["patch"].Value);
            var url = minor < 4 || (minor == 4 && patch < 38) ? " " + WebUtility.HtmlEncode(RequestProfiler.LocalPath(target)) : string.Empty;
            var (name, port) = NameAndPort(host);
            return Page(
                server,
                "text/html; charset=iso-8859-1",
                $"<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n<html><head>\n<title>404 Not Found</title>\n</head><body>\n<h1>Not Found</h1>\n<p>The requested URL{url} was not found on this server.</p>\n<hr>\n<address>{server} Server at {name} Port {port}</address>\n</body></html>\n");
        }

        if (server.StartsWith("Microsoft-IIS/", StringComparison.Ordinal))
            return new GeneratedResponse(StatusCodes.Status404NotFound, Headers(server, ("Content-Type", "text/html"), ("X-Powered-By", "ASP.NET")), IisNotFound);

        return server.StartsWith("lighttpd/", StringComparison.Ordinal)
            ? Page(server, "text/html", LighttpdNotFound)
            : null;
    }

    private static GeneratedResponse Page(string server, string contentType, string body)
    {
        return new GeneratedResponse(StatusCodes.Status404NotFound, Headers(server, ("Content-Type", contentType)), body);
    }

    private static List<ResponseHeader> Headers(string server, params (string Name, string Value)[] headers)
    {
        List<ResponseHeader> result =
        [
            ..headers.Select(x => new ResponseHeader
            {
                Name = x.Name,
                Value = x.Value
            })
        ];
        if (server.Length > 0)
            result.Add(new ResponseHeader
            {
                Name = "Server",
                Value = server
            });
        return result;
    }

    private static (string Name, int Port) NameAndPort(string host)
    {
        var value = host.Trim();
        var colon = value.LastIndexOf(':');
        return colon > 0 && value.IndexOf(':') == colon && int.TryParse(value[(colon + 1)..], out var port)
            ? (value[..colon], port)
            : (value, 80);
    }

    private static byte[] Hash(string value)
    {
        return SHA1.HashData(Encoding.UTF8.GetBytes(value));
    }

    [GeneratedRegex(@"/\.git/(?<file>HEAD|ORIG_HEAD|refs/(?:heads|remotes|tags)/[^/]+(?:/[^/]+)*)$", RegexOptions.CultureInvariant)]
    private static partial Regex GitRefRegex();

    [GeneratedRegex(@"^Apache/2\.(?<minor>\d+)\.(?<patch>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex ApacheRegex();
}