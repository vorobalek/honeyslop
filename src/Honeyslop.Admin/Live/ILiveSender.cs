namespace Honeyslop.Admin.Live;

public interface ILiveSender
{
    Task SendAsync(string connectionId, string method, object?[] arguments, CancellationToken cancellationToken);
}
