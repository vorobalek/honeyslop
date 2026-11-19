namespace Honeyslop.Admin.Core.Contracts;

public sealed record ClientDelayChangeDto(IReadOnlyDictionary<string, string> Errors,
    int? ResponseDelayMilliseconds)
{
    public bool Changed => Errors.Count == 0;
}