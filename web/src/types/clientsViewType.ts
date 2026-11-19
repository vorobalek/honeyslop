import { ClientActivityType } from './clientActivityType';
import { CountriesType } from './countriesType';
import { PagedType } from './pagedType';

export interface ClientsViewType {
    clients: PagedType<ClientActivityType>;
    countries: CountriesType;
    asOf: string;
}
