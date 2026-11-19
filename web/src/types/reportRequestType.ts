import { ReportOutcomeType } from './reportOutcomeType';

export interface ReportRequestType {
    outcome: ReportOutcomeType;
    reportId: number | null;
}
