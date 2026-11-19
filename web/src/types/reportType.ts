import { AbuseReportStateType } from './abuseReportStateType';

export interface ReportType {
    id: number;
    clientIp: string;
    recipients: string[];
    subject: string;
    state: AbuseReportStateType;
    requestedBy: string | null;
    attempts: number;
    attachedRequests: number | null;
    error: string | null;
    createdAt: string;
    sentAt: string | null;
    retryAt: string | null;
}
