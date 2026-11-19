export interface BouncedContactType {
    contact: string;
    bounces: number;
    reports: number;
    lastBouncedAt: string;
    lastReportId: number;
    lastStatus: string | null;
    lastDiagnostic: string | null;
    blocked: boolean;
}
