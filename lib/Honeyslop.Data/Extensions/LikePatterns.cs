namespace Honeyslop.Data.Extensions;

internal static class LikePatterns
{
    public const string Escape = "\\";

    extension(string value)
    {
        public string ContainsPattern =>
            "%" +
            value
                .Replace(Escape, Escape + Escape)
                .Replace("%", Escape + "%")
                .Replace("_", Escape + "_") +
            "%";
    }
}