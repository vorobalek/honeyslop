import { RequestOutcomeType } from './requestOutcomeType';

export interface RequestFilterType {
    clientIp: string | null;
    host: string | null;
    target: string | null;
    outcome: RequestOutcomeType | null;
    entryId: number | null;
}
