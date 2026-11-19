import { CountryType } from './countryType';
import { EntryStateType } from './entryStateType';
import { EntryType } from './entryType';
import { GenerationOutputType } from './generationOutputType';

export interface EntryViewType {
    entry: EntryType;
    state: EntryStateType;
    canPreview: boolean;
    country: CountryType | null;
    currentId: number | null;
    output: GenerationOutputType | null;
    defaultResponseDelay: string;
    asOf: string;
}
