import { RequestOutcomeType } from './requestOutcomeType';

export interface RequestRecordType {
    id: number;
    receivedAt: string;
    clientIp: string;
    method: string;
    host: string;
    target: string;
    userAgent: string | null;
    headers: string | null;
    body: string | null;
    outcome: RequestOutcomeType;
    responseStatus: number;
    entryId: number | null;
    durationMs: number;
    instance: string | null;
}
