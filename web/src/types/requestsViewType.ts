import { CountriesType } from './countriesType';
import { PagedType } from './pagedType';
import { RequestRowType } from './requestRowType';

export interface RequestsViewType {
    requests: PagedType<RequestRowType>;
    countries: CountriesType;
    delays: Record<string, number>;
    learnedDelays: Record<string, number>;
    asOf: string;
}
