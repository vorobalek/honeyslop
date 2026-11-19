import { useQuery } from '../../hooks/useLocation';
import { useTitle } from '../../hooks/useTitle';
import { useWatch } from '../../hooks/useWatch';
import { useWatchPresence } from '../../hooks/useWatched';
import { REQUEST_COLUMNS, REQUEST_OUTCOMES } from '../../lib/constants';
import { oneOf, pageParam, sortingParam, textParam } from '../../lib/listQuery';
import Link from '../Common/Link';
import PageNotice from '../Common/PageNotice';
import Presence from '../Common/Presence';
import RequestFilters from '../Filters/RequestFilters';
import RequestsPanel from '../Requests/RequestsPanel';
import ClientHeading from './ClientHeading';
import ClientMeta from './ClientMeta';
import WhoisPanel from './WhoisPanel';
import React, { useMemo } from 'react';

type ClientPageProps = {
    ip: string;
};

const ClientPage: React.FC<ClientPageProps> = ({ ip }) => {
    useTitle(ip);
    const query = useQuery();
    const { host, target, outcome, sorting, page } = useMemo(
        () => ({
            host: textParam(query, 'Host'),
            target: textParam(query, 'Target'),
            outcome: oneOf(query, 'Outcome', REQUEST_OUTCOMES),
            sorting: sortingParam(query, REQUEST_COLUMNS),
            page: pageParam(query),
        }),
        [query],
    );
    const path = `/clients/${encodeURIComponent(ip)}`;
    const filter = useMemo(
        () => ({ clientIp: ip, host: host || null, target: target || null, outcome, entryId: null }),
        [ip, host, target, outcome],
    );
    const links = useMemo(() => ({ Host: host, Target: target, Outcome: outcome }), [host, target, outcome]);
    const watch = useWatch({ view: 'client', ip });
    const presence = useWatchPresence(watch);

    if (presence !== 'present') return <Presence presence={presence} />;
    return (
        <>
            <p className='back'>
                <Link to='/clients'>← All clients</Link>
            </p>
            <PageNotice />
            <ClientHeading watch={watch} />
            <ClientMeta watch={watch} />
            <h2>Network</h2>
            <WhoisPanel ip={ip} />
            <h2>Requests</h2>
            <RequestFilters
                path={path}
                clientIp={ip}
                clientIpLocked
                host={host}
                target={target}
                outcome={outcome}
                entryId={null}
            />
            <RequestsPanel
                path={path}
                filter={filter}
                links={links}
                sorting={sorting}
                page={page}
            />
        </>
    );
};

export default React.memo(ClientPage);
