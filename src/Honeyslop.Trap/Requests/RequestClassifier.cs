using Honeyslop.Trap.Options;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Requests;

internal sealed class RequestClassifier(IOptions<TrapOptions> options) : IRequestClassifier
{
    public Classification Classify(string method, string target)
    {
        var normalizedMethod = NormalizeMethod(method);
        if (normalizedMethod is null)
            return new Classification(ClassificationKind.MethodNotAllowed, method);

        if (target.Length == 0 || target[0] != '/')
            return new Classification(ClassificationKind.BadRequest, normalizedMethod);

        if (target.Length > options.Value.MaxTargetLength)
            return new Classification(ClassificationKind.UriTooLong, normalizedMethod);

        var extension = GetExtension(target);
        return extension is not null && options.Value.StaticExtensions.Contains(extension)
            ? new Classification(ClassificationKind.StaticFile, normalizedMethod)
            : new Classification(ClassificationKind.Generate, normalizedMethod);
    }

    private static string? NormalizeMethod(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" or "HEAD" => "GET",
            "POST" => "POST",
            "PUT" => "PUT",
            "PATCH" => "PATCH",
            "DELETE" => "DELETE",
            _ => null
        };
    }

    private static string? GetExtension(string target)
    {
        var pathEnd = target.IndexOfAny(['?', '#']);
        var path = pathEnd >= 0 ? target[..pathEnd] : target;
        var segment = Uri.UnescapeDataString(path[(path.LastIndexOf('/') + 1)..]);
        var dot = segment.LastIndexOf('.');
        return dot >= 0 && dot < segment.Length - 1
            ? segment[(dot + 1)..].TrimEnd().ToLowerInvariant()
            : null;
    }
}
