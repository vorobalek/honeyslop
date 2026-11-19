export function expiredNotice(count: number): string {
    return `Expired ${count} ${count === 1 ? 'entry' : 'entries'}.`;
}
