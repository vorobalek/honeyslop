import { ClientAbuseType } from './clientAbuseType';
import { ClientType } from './clientType';
import { ClientsQueryType } from './clientsQueryType';
import { ClientsViewType } from './clientsViewType';
import { EntriesQueryType } from './entriesQueryType';
import { EntriesViewType } from './entriesViewType';
import { EntryViewType } from './entryViewType';
import { GeneratorsViewType } from './generatorsViewType';
import { ReportDetailsType } from './reportDetailsType';
import { ReportsQueryType } from './reportsQueryType';
import { ReportsViewType } from './reportsViewType';
import { RequestDetailsType } from './requestDetailsType';
import { RequestsQueryType } from './requestsQueryType';
import { RequestsViewType } from './requestsViewType';
import { SettingsViewType } from './settingsViewType';

interface WatchViewsType {
    entries: { request: { query: EntriesQueryType }; view: EntriesViewType };
    entry: { request: { id: number }; view: EntryViewType };
    requests: { request: { query: RequestsQueryType }; view: RequestsViewType };
    request: { request: { id: number }; view: RequestDetailsType };
    clients: { request: { query: ClientsQueryType }; view: ClientsViewType };
    client: { request: { ip: string }; view: ClientType };
    clientAbuse: { request: { ip: string }; view: ClientAbuseType };
    reports: { request: { query: ReportsQueryType }; view: ReportsViewType };
    report: { request: { id: number }; view: ReportDetailsType };
    generators: { request: Record<never, never>; view: GeneratorsViewType };
    settings: { request: Record<never, never>; view: SettingsViewType };
}

export type WatchNameType = keyof WatchViewsType;

export type WatchRequestType<N extends WatchNameType = WatchNameType> = { view: N } & WatchViewsType[N]['request'];

export type WatchViewType<N extends WatchNameType> = WatchViewsType[N]['view'];
