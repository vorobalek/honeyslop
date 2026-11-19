using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Llm.Contracts;

namespace Honeyslop.Admin.Views.Contracts;

public sealed record EntryViewDto(
    Entry Entry,
    EntryState State,
    bool CanPreview,
    CountryDto? Country,
    long? CurrentId,
    GenerationOutputDto? Output,
    int DefaultResponseDelayMilliseconds,
    DateTimeOffset AsOf);