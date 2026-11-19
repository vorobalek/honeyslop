import { secondsBetween } from '../../lib/time';
import { EntryRowType } from '../../types/entryRowType';

export function runningFor(entry: EntryRowType, asOf: string): number {
    return secondsBetween(entry.claimedAt ?? asOf, asOf);
}

export function waitedBefore(entry: EntryRowType, asOf: string): number {
    return secondsBetween(entry.createdAt, entry.claimedAt ?? asOf);
}

export function queuedFor(entry: EntryRowType, asOf: string): number {
    return secondsBetween(entry.createdAt, asOf);
}

export function requestedAgo(entry: EntryRowType, asOf: string): number | null {
    return entry.requestedAt ? secondsBetween(entry.requestedAt, asOf) : null;
}
