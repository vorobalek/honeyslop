using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ClientNoiseChangeDto(IReadOnlyDictionary<string, string> Errors,
    NoiseTier? Noise)
{
    public bool Changed => Errors.Count == 0;
}