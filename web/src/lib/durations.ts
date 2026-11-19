export type DurationUnit = 'Seconds' | 'Minutes' | 'Hours' | 'Days';

export const DURATION_UNITS: readonly DurationUnit[] = ['Seconds', 'Minutes', 'Hours', 'Days'];

const UNIT_SECONDS: Record<DurationUnit, number> = { Seconds: 1, Minutes: 60, Hours: 3600, Days: 86400 };

export type Duration = {
    value: number;
    unit: DurationUnit;
};

export function splitDuration(totalSeconds: number): Duration {
    const whole = Math.trunc(totalSeconds);
    if (whole === 0) return { value: 0, unit: 'Days' };
    if (whole % 60 !== 0) return { value: whole, unit: 'Seconds' };
    const minutes = whole / 60;
    if (minutes % (24 * 60) === 0) return { value: minutes / (24 * 60), unit: 'Days' };
    return minutes % 60 === 0 ? { value: minutes / 60, unit: 'Hours' } : { value: minutes, unit: 'Minutes' };
}

export function joinDuration(value: FormDataEntryValue | null, unit: FormDataEntryValue | null): number {
    const number = Number.parseInt(String(value ?? ''), 10);
    const seconds = UNIT_SECONDS[(unit as DurationUnit) ?? 'Days'] ?? UNIT_SECONDS.Days;
    return (Number.isFinite(number) ? number : 0) * seconds;
}
