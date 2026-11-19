using Honeyslop.Data.Contracts;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record SettingsDto(HoneyslopSettings Settings,
    string DefaultUserAgent,
    int DefaultResponseDelayMilliseconds,
    string? DefaultSenderName,
    string? PersonalSenderName);