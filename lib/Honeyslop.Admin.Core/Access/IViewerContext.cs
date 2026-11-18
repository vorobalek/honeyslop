using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Core.Access;

public interface IViewerContext
{
    ViewerDto Viewer { get; }
}
