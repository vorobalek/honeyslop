import { AbuseReportStateType } from '../types/abuseReportStateType';
import { EntryStateType } from '../types/entryStateType';
import { RequestOutcomeType } from '../types/requestOutcomeType';

export function entryStateBadge(state: EntryStateType): string {
    switch (state) {
        case 'Ready':
            return 'badge badge-ready';
        case 'Generating':
            return 'badge badge-generated';
        case 'Queued':
            return 'badge badge-pending';
        case 'Expired':
            return 'badge badge-static';
        default:
            return 'badge badge-failed';
    }
}

export function outcomeBadge(outcome: RequestOutcomeType): string {
    switch (outcome) {
        case 'Hit':
            return 'badge badge-ready';
        case 'Generated':
        case 'Waiting':
            return 'badge badge-generated';
        case 'Fallback':
        case 'Abandoned':
            return 'badge badge-pending';
        case 'Error':
        case 'Noise':
            return 'badge badge-failed';
        default:
            return 'badge badge-static';
    }
}

export function reportStateBadge(state: AbuseReportStateType): string {
    switch (state) {
        case 'Sent':
            return 'badge badge-ready';
        case 'Failed':
            return 'badge badge-failed';
        case 'Sending':
            return 'badge badge-generated';
        default:
            return 'badge badge-pending';
    }
}
