import { CountriesType } from './countriesType';
import { PagedType } from './pagedType';
import { ReportType } from './reportType';

export interface ReportsViewType {
    reports: PagedType<ReportType>;
    countries: CountriesType;
    asOf: string;
}
