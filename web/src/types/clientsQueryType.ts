import { ClientColumnType } from './clientColumnType';
import { SortingType } from './sortingType';

export interface ClientsQueryType {
    days: number;
    sort: SortingType<ClientColumnType> | null;
    page: number;
}
