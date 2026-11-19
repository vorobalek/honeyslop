import { useWatch } from '../../hooks/useWatch';
import { useWatchPresence } from '../../hooks/useWatched';
import { QueryValues } from '../../lib/listQuery';
import { RequestOutcomeType } from '../../types/requestOutcomeType';
import { RequestsViewType } from '../../types/requestsViewType';
import { SortingType } from '../../types/sortingType';
import ListPager from '../Common/ListPager';
import Presence from '../Common/Presence';
import RequestsCount from './RequestsCount';
import RequestsTable from './RequestsTable';
import React from 'react';

export type RequestsFilter = {
    clientIp: string | null;
    host: string | null;
    target: string | null;
    outcome: RequestOutcomeType | null;
    entryId: number | null;
};

type RequestsPanelProps = {
    path: string;
    filter: RequestsFilter;
    links: QueryValues;
    sorting: SortingType<string> | null;
    page: number;
};

const selectRequests = (view: RequestsViewType) => view.requests;

const RequestsPanel: React.FC<RequestsPanelProps> = ({ path, filter, links, sorting, page }) => {
    const watch = useWatch<RequestsViewType>('watchRequests', { filter, sort: sorting, page });
    const presence = useWatchPresence(watch);
    if (presence !== 'present') return <Presence presence={presence} />;
    return (
        <div>
            <div className='actions'>
                <RequestsCount watch={watch} />
            </div>
            <RequestsTable
                watch={watch}
                path={path}
                links={links}
                sorting={sorting}
            />
            <ListPager
                watch={watch}
                select={selectRequests}
                path={path}
                filter={links}
                sorting={sorting}
                newer='← Newer'
            />
        </div>
    );
};

export default React.memo(RequestsPanel);
