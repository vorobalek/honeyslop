import { CountryType } from './countryType';
import { ReportType } from './reportType';

export interface ReportDetailsType {
    report: ReportType;
    country: CountryType | null;
    body: string;
    messageId: string | null;
    from: string | null;
    fromIsExpected: boolean;
}
