import { AbuseDraftType } from './abuseDraftType';
import { ReportType } from './reportType';
import { WhoisType } from './whoisType';

export interface ClientAbuseType {
    ip: string;
    whois: WhoisType | null;
    reports: ReportType[];
    newSinceLastSent: number;
    draft: AbuseDraftType | null;
    recipients: string[];
    lastReport: ReportType | null;
    lastSent: ReportType | null;
    reportPending: boolean;
    alreadyReported: boolean;
}
