import { CountryType } from './countryType';
import { RequestRecordType } from './requestRecordType';

export interface RequestDetailsType {
    request: RequestRecordType;
    country: CountryType | null;
}
