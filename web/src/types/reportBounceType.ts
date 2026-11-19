import { BounceActionType } from './bounceActionType';

export interface ReportBounceType {
    recipient: string;
    action: BounceActionType;
    status: string | null;
    diagnostic: string | null;
    mta: string | null;
    bouncedAt: string;
}
