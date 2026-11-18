using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Core.Access;

internal interface IAccessGuard
{
    ViewerDto Viewer { get; }
    Task EnsureCanReadAsync(CancellationToken cancellationToken);
    void EnsureCanOperate();
}
