using System.Globalization;

namespace Honeyslop.Admin.Core.Text;

public static class TextExtensions
{
    extension(DateTimeOffset value)
    {
        public string Display => value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    extension(int count)
    {
        public string Of(string noun)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{count} {noun}{(count == 1 ? string.Empty : "s")}");
        }
    }

    extension(DateTimeOffset? value)
    {
        public string DisplayOrDash()
        {
            return value?.Display ?? "—";
        }
    }
}
