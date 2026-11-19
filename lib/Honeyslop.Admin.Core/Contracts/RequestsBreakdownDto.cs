using Honeyslop.Admin.Core.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record RequestsBreakdownDto(BreakdownDimension By,
    int Requests,
    RequestGroupDto[] Groups,
    DateTimeOffset AsOf);