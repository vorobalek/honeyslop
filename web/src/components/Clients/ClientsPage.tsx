import { useQuery } from '../../hooks/useLocation';
import { useTitle } from '../../hooks/useTitle';
import { useWatch } from '../../hooks/useWatch';
import { useWatchPresence } from '../../hooks/useWatched';
import { CLIENT_COLUMNS } from '../../lib/constants';
import { numberParam, pageParam, sortingParam } from '../../lib/listQuery';
import { ClientsViewType } from '../../types/clientsViewType';
import ListPager from '../Common/ListPager';
import Presence from '../Common/Presence';
import DaysFilter from '../Filters/DaysFilter';
import ClientsCount from './ClientsCount';
import ClientsTable from './ClientsTable';
import React, { useMemo } from 'react';

const selectClients = (view: ClientsViewType) => view.clients;

const ClientsPage: React.FC = () => {
    useTitle('Clients');
    const query = useQuery();
    const days = numberParam(query, 'Days') ?? 7;
    const sorting = useMemo(() => sortingParam(query, CLIENT_COLUMNS), [query]);
    const links = useMemo(() => ({ Days: days }), [days]);
    const watch = useWatch<ClientsViewType>('watchClients', { days, sort: sorting, page: pageParam(query) });
    const presence = useWatchPresence(watch);
    if (presence !== 'present') return <Presence presence={presence} />;
    return (
        <>
            <DaysFilter days={days} />
            <div className='actions'>
                <ClientsCount watch={watch} />
            </div>
            <ClientsTable
                watch={watch}
                links={links}
                sorting={sorting}
            />
            <ListPager
                watch={watch}
                select={selectClients}
                path='/clients'
                filter={links}
                sorting={sorting}
                newer='← More recent'
            />
        </>
    );
};

export default React.memo(ClientsPage);
