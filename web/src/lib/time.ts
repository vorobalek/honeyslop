function utc(value: string): string {
    return new Date(value).toISOString();
}

export function display(value: string): string {
    return utc(value).slice(0, 19).replace('T', ' ');
}

export function displayOrDash(value: string | null | undefined): string {
    return value ? display(value) : '—';
}

export function clock(value: string): string {
    return utc(value).slice(11, 19);
}

export function secondsBetween(from: string, to: string): number {
    return (new Date(to).getTime() - new Date(from).getTime()) / 1000;
}

export function duration(totalSeconds: number): string {
    if (totalSeconds < 0) return '0 s';
    const whole = Math.floor(totalSeconds);
    if (totalSeconds < 60) return `${whole} s`;
    if (totalSeconds < 3600) return `${Math.floor(whole / 60)} min ${whole % 60} s`;
    return `${Math.floor(whole / 3600)} h ${Math.floor(whole / 60) % 60} min`;
}

export function timeSpanSeconds(value: string): number {
    const match = /^(-)?(?:(\d+)\.)?(\d+):(\d+):(\d+)(?:\.\d+)?$/.exec(value);
    if (match === null) return 0;
    const [, negative, days, hours, minutes, seconds] = match;
    const total = Number(days ?? 0) * 86400 + Number(hours) * 3600 + Number(minutes) * 60 + Number(seconds);
    return negative ? -total : total;
}
