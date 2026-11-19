namespace Honeyslop.Admin.Core.Contracts;

public sealed record EntryChangeDto(IReadOnlyDictionary<string, string> Errors, long[] Ids)
{
    public bool Changed => Errors.Count == 0;
}
