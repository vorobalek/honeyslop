namespace Honeyslop.Admin.Core.Contracts;

public sealed record CountryDto(string Code, string Name)
{
    private const int RegionalIndicatorA = 0x1F1E6;

    public string Flag => string.Concat(Code.Select(x => char.ConvertFromUtf32(RegionalIndicatorA + x - 'A')));
}
