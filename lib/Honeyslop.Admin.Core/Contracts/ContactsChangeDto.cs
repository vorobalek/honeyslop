namespace Honeyslop.Admin.Core.Contracts;

public sealed record ContactsChangeDto(IReadOnlyDictionary<string, string> Errors,
    int Changed)
{
    public bool Done => Errors.Count == 0;
}