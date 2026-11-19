import { CountryType } from './countryType';
import { ReportBounceType } from './reportBounceType';
import { ReportType } from './reportType';

export interface ReportDetailsType {
    report: ReportType;
    country: CountryType | null;
    body: string;
    messageId: string | null;
    from: string | null;
    fromIsExpected: boolean;
    bounces: ReportBounceType[];
}
