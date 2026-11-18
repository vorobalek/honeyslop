using Honeyslop.Data.Entities;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record RequestDetailsDto(RequestRecord Request, CountryDto? Country);
