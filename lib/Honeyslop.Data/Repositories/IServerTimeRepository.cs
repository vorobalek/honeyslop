namespace Honeyslop.Data.Repositories;

public interface IServerTimeRepository
{
    Task<DateTimeOffset> GetNowAsync(CancellationToken cancellationToken);
}