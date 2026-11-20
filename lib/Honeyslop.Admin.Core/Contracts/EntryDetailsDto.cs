using Honeyslop.Data.Entities;
using Honeyslop.Llm.Contracts;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record EntryDetailsDto(
    Entry Entry,
    CountryDto? Country,
    long? CurrentId,
    GenerationOutputDto? Output,
    int DefaultResponseDelayMilliseconds,
    bool NoiseEnabled,
    DateTimeOffset AsOf);