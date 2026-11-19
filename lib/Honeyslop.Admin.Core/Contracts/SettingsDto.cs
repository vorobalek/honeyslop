using Honeyslop.Data.Contracts;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record SettingsDto(HoneyslopSettings Settings,
    string DefaultUserAgent,
    TimeSpan DefaultResponseDelay,
    string? DefaultSenderName,
    string? PersonalSenderName);