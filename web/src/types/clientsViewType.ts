import { ClientActivityType } from './clientActivityType';
import { CountriesType } from './countriesType';
import { PagedType } from './pagedType';

export interface ClientsViewType {
    clients: PagedType<ClientActivityType>;
    countries: CountriesType;
    delays: Record<string, number>;
    learnedDelays: Record<string, number>;
    asOf: string;
}
