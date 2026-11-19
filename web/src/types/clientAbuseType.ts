import { AbuseDraftType } from './abuseDraftType';
import { BouncedContactType } from './bouncedContactType';
import { ReportType } from './reportType';
import { WhoisType } from './whoisType';

export interface ClientAbuseType {
    ip: string;
    whois: WhoisType | null;
    reports: ReportType[];
    newSinceLastSent: number;
    draft: AbuseDraftType | null;
    blockedRecipients: string[];
    bounces: BouncedContactType[];
    recipients: string[];
    lastReport: ReportType | null;
    lastSent: ReportType | null;
    reportPending: boolean;
    alreadyReported: boolean;
}
