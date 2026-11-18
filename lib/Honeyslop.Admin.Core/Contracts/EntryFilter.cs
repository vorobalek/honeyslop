using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record EntryFilter(
    string? Host = null,
    string? Target = null,
    EntryState? State = null)
{
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Host) &&
        string.IsNullOrWhiteSpace(Target) &&
        State is null;
}
