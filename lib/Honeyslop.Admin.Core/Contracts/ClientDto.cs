using Honeyslop.Data.Contracts;
using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ClientDto(string Ip,
    CountryDto? Country,
    ClientActivity? Activity,
    int? ResponseDelayMilliseconds,
    int? LearnedDelayMilliseconds,
    int? LearnedTimeoutMilliseconds,
    bool NoiseEnabled,
    NoiseTier? Noise);