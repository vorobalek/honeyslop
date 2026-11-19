using Honeyslop.Data.Contracts;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ClientDto(string Ip,
    CountryDto? Country,
    ClientActivity? Activity,
    int? ResponseDelayMilliseconds);