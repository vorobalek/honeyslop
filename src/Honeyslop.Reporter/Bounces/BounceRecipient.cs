using Honeyslop.Data.Enums;

namespace Honeyslop.Reporter.Bounces;

internal sealed record BounceRecipient(string Address, BounceAction Action, string? Status, string? Diagnostic, string? Mta);