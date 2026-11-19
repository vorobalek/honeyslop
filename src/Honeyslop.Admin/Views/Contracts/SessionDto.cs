namespace Honeyslop.Admin.Views.Contracts;

public sealed record SessionDto(bool SignInEnabled,
    bool SignedIn,
    string? Name,
    string? UserId,
    bool CanOperate);