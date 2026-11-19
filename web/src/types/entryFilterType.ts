import { EntryStateType } from './entryStateType';

export interface EntryFilterType {
    host: string | null;
    target: string | null;
    state: EntryStateType | null;
}
