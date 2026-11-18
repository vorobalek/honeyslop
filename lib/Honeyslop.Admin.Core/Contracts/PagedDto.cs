namespace Honeyslop.Admin.Core.Contracts;

public sealed record PagedDto<T>(T[] Items, int Total, int Page, int PageSize)
{
    public int PageCount => Math.Max(1, (Total + PageSize - 1) / PageSize);
}
