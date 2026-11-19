import { ClientColumnType } from '../types/clientColumnType';
import { EntryColumnType } from '../types/entryColumnType';
import { EntryStateType } from '../types/entryStateType';
import { ReportColumnType } from '../types/reportColumnType';
import { RequestColumnType } from '../types/requestColumnType';
import { RequestOutcomeType } from '../types/requestOutcomeType';

export const ENTRY_STATES: readonly EntryStateType[] = ['Queued', 'Generating', 'Ready', 'Failed', 'Expired'];

export const REQUEST_OUTCOMES: readonly RequestOutcomeType[] = [
    'Hit',
    'Generated',
    'Fallback',
    'Static',
    'Abandoned',
    'Error',
    'Waiting',
];

export const ENTRY_COLUMNS: readonly EntryColumnType[] = [
    'Status',
    'Method',
    'Host',
    'Path',
    'Code',
    'Size',
    'Hits',
    'Created',
    'LastHit',
];

export const REQUEST_COLUMNS: readonly RequestColumnType[] = [
    'Time',
    'Client',
    'Method',
    'Host',
    'Path',
    'Outcome',
    'Code',
    'Duration',
    'UserAgent',
];

export const CLIENT_COLUMNS: readonly ClientColumnType[] = [
    'Client',
    'Requests',
    'Hosts',
    'Paths',
    'FirstSeen',
    'LastSeen',
    'UserAgent',
];

export const REPORT_COLUMNS: readonly ReportColumnType[] = [
    'Requested',
    'Client',
    'By',
    'To',
    'Status',
    'Sent',
    'Requests',
    'Attempts',
    'Error',
];

export const SENDER_NAME_MAX_LENGTH = 128;
export const USER_AGENT_MAX_LENGTH = 512;
export const GUIDANCE_MAX_LENGTH = 2048;
export const RESPONSE_BODY_MAX_LENGTH = 65536;
export const FILTER_DELAY = 400;
