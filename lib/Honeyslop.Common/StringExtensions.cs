namespace Honeyslop.Common;

public static class StringExtensions
{
    extension(string value)
    {
        public string Truncate(int maxLength)
        {
            return value.Length <= maxLength
                ? value
                : value[..maxLength];
        }
    }
}