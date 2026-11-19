import { ClientActivityType } from './clientActivityType';
import { CountryType } from './countryType';

export interface ClientType {
    ip: string;
    country: CountryType | null;
    activity: ClientActivityType | null;
    responseDelayMilliseconds: number | null;
    learnedDelayMilliseconds: number | null;
    learnedTimeoutMilliseconds: number | null;
}
