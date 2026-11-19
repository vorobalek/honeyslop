import { ReportColumnType } from './reportColumnType';
import { SortingType } from './sortingType';

export interface ReportsQueryType {
    sort: SortingType<ReportColumnType> | null;
    page: number;
}
