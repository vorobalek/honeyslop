using System.Linq.Expressions;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Data.Extensions;

internal static class SortingExtensions
{
    public static IOrderedQueryable<T> SortBy<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> key,
        SortDirection direction)
    {
        return direction == SortDirection.Asc
            ? query.OrderBy(key)
            : query.OrderByDescending(key);
    }
}
