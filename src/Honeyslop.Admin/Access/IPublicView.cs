namespace Honeyslop.Admin.Access;

public interface IPublicView
{
    ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken);
    void Reset();
}
