namespace Honeyslop.Admin.Core.Contracts;

public sealed record ViewerDto(string? UserId,
    string? Name,
    bool CanOperate)
{
    public static readonly ViewerDto Anonymous = new(null, null, false);
}