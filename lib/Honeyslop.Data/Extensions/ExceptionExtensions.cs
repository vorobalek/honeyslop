using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Honeyslop.Data.Extensions;

internal static class ExceptionExtensions
{
    extension(DbUpdateException exception)
    {
        public bool IsUniqueViolation =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            };
    }
}
