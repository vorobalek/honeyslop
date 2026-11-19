import { RequestOutcomeType } from './requestOutcomeType';

export interface RequestRowType {
    id: number;
    receivedAt: string;
    clientIp: string;
    method: string;
    host: string;
    target: string;
    entryId: number | null;
    outcome: RequestOutcomeType;
    responseStatus: number;
    durationMs: number;
    userAgent: string | null;
}
