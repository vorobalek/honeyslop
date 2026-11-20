using Honeyslop.Data.Enums;

namespace Honeyslop.Trap.Settings;

public readonly record struct PersonalSettings(TimeSpan? ManualDelay,
    TimeSpan? LearnedDelay,
    NoiseTier? Noise);