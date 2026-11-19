namespace Honeyslop.Trap.Settings;

public readonly record struct PersonalDelay(TimeSpan? Manual,
    TimeSpan? Learned);