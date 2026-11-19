import { WhoisOriginType } from './whoisOriginType';

export interface WhoisType {
    prefix: string | null;
    origins: WhoisOriginType[];
    network: string | null;
    networkName: string | null;
    owner: string | null;
    country: string | null;
    registry: string | null;
    abuseContacts: string[];
    records: string;
    lookedUpAt: string;
}
