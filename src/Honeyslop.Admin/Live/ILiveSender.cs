namespace Honeyslop.Admin.Live;

public interface ILiveSender
{
    Task JoinAsync(string connectionId, string group, CancellationToken cancellationToken);
    Task LeaveAsync(string connectionId, string group, CancellationToken cancellationToken);
    Task SendAsync(string group, string method, object?[] arguments, CancellationToken cancellationToken);
}
