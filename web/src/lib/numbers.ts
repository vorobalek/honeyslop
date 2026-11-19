function upToOneDecimal(value: number): string {
    return String(Math.round(value * 10) / 10);
}

export function oneDecimal(value: number): string {
    return value.toFixed(1);
}

export function grouped(value: number): string {
    return Math.round(value).toLocaleString('en-US');
}

export function size(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${upToOneDecimal(bytes / 1024)} KB`;
    return `${upToOneDecimal(bytes / 1024 / 1024)} MB`;
}

export function countOf(count: number, noun: string): string {
    return `${count} ${noun}${count === 1 ? '' : 's'}`;
}
