import { EntryRowType } from './entryRowType';
import { EntryTotalsType } from './entryTotalsType';
import { GeneratorType } from './generatorType';

export interface GeneratorsViewType {
    configured: boolean;
    generators: GeneratorType[];
    totals: EntryTotalsType;
    oldestQueuedAt: string | null;
    generating: EntryRowType[];
    queued: EntryRowType[];
    totalSlots: number;
    busySlots: number;
    asOf: string;
}
