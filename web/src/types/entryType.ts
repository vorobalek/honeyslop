import { EntryStatusType } from './entryStatusType';
import { ResponseHeaderType } from './responseHeaderType';

export interface EntryType {
    id: number;
    method: string;
    host: string;
    target: string;
    status: EntryStatusType;
    attempts: number;
    claimedBy: string | null;
    claimedAt: string | null;
    error: string | null;
    clientIp: string | null;
    userAgent: string | null;
    requestHeaders: string | null;
    requestBody: string | null;
    responseStatus: number | null;
    responseHeaders: ResponseHeaderType[];
    responseBody: string | null;
    model: string | null;
    generator: string | null;
    promptTokens: number | null;
    completionTokens: number | null;
    generationMs: number | null;
    hits: number;
    createdAt: string;
    updatedAt: string;
    generatedAt: string | null;
    lastHitAt: string | null;
    requestedAt: string | null;
    guidance: string | null;
    changedBy: string | null;
    responseDelayMilliseconds: number | null;
}
