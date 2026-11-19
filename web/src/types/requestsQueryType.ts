import { RequestColumnType } from './requestColumnType';
import { RequestFilterType } from './requestFilterType';
import { SortingType } from './sortingType';

export interface RequestsQueryType {
    filter: RequestFilterType;
    sort: SortingType<RequestColumnType> | null;
    page: number;
}
