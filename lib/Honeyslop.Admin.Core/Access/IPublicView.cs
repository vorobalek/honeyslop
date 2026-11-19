namespace Honeyslop.Admin.Core.Access;

public interface IPublicView
{
    ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken);
    void Reset();
}