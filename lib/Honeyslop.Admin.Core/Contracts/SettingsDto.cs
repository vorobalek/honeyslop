using Honeyslop.Data.Contracts;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record SettingsDto(HoneyslopSettings Settings, string DefaultUserAgent, string? DefaultSenderName, string? PersonalSenderName);
