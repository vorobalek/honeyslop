using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Requests;

public sealed class RequestRules
{
    public const int DefaultMaxTargetLength = 1024;

    public int MaxTargetLength { get; set; } = DefaultMaxTargetLength;
    public IReadOnlySet<string> StaticExtensions { get; set; } = StaticFileExtensions.Default;

    public void Apply(int maxTargetLength, string? staticExtensions)
    {
        MaxTargetLength = Math.Clamp(maxTargetLength, 1, Entry.TargetMaxLength);
        StaticExtensions = staticExtensions is null ? StaticFileExtensions.Default : StaticFileExtensions.Parse(staticExtensions);
    }

    public Classification Classify(string method, string target)
    {
        var normalizedMethod = NormalizeMethod(method);
        if (normalizedMethod is null)
            return new Classification(ClassificationKind.MethodNotAllowed, method);

        if (target.Length == 0 || target[0] != '/')
            return new Classification(ClassificationKind.BadRequest, normalizedMethod);

        if (target.Length > MaxTargetLength)
            return new Classification(ClassificationKind.UriTooLong, normalizedMethod);

        var extension = ExtensionOf(target);
        return extension is not null && StaticExtensions.Contains(extension)
            ? new Classification(ClassificationKind.StaticFile, normalizedMethod)
            : new Classification(ClassificationKind.Generate, normalizedMethod);
    }

    public static string? NormalizeMethod(string method)
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

    private static string? ExtensionOf(string target)
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