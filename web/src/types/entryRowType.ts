import { EntryStateType } from './entryStateType';

export interface EntryRowType {
    id: number;
    state: EntryStateType;
    method: string;
    host: string;
    target: string;
    responseStatus: number | null;
    bodyLength: number;
    hits: number;
    attempts: number;
    generator: string | null;
    error: string | null;
    createdAt: string;
    claimedAt: string | null;
    requestedAt: string | null;
    lastHitAt: string | null;
}
