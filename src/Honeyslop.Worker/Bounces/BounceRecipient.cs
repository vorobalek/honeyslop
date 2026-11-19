using Honeyslop.Data.Enums;

namespace Honeyslop.Worker.Bounces;

internal sealed record BounceRecipient(string Address,
    BounceAction Action,
    string? Status,
    string? Diagnostic,
    string? Mta);