import { EntryColumnType } from './entryColumnType';
import { EntryFilterType } from './entryFilterType';
import { SortingType } from './sortingType';

export interface EntriesQueryType {
    filter: EntryFilterType;
    sort: SortingType<EntryColumnType> | null;
    page: number;
}
