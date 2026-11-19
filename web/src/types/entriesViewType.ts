import { EntryRowType } from './entryRowType';
import { EntryTotalsType } from './entryTotalsType';
import { PagedType } from './pagedType';

export interface EntriesViewType {
    totals: EntryTotalsType;
    entries: PagedType<EntryRowType>;
    asOf: string;
}
