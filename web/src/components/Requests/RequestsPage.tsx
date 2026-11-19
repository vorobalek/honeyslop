import { useQuery } from '../../hooks/useLocation';
import { useTitle } from '../../hooks/useTitle';
import { REQUEST_COLUMNS, REQUEST_OUTCOMES } from '../../lib/constants';
import { numberParam, oneOf, pageParam, sortingParam, textParam } from '../../lib/listQuery';
import Link from '../Common/Link';
import RequestFilters from '../Filters/RequestFilters';
import RequestsPanel from './RequestsPanel';
import React, { useMemo } from 'react';

const RequestsPage: React.FC = () => {
    useTitle('Requests');
    const query = useQuery();
    const params = useMemo(
        () => ({
            clientIp: textParam(query, 'ClientIp'),
            host: textParam(query, 'Host'),
            target: textParam(query, 'Target'),
            outcome: oneOf(query, 'Outcome', REQUEST_OUTCOMES),
            entryId: numberParam(query, 'EntryId'),
            sorting: sortingParam(query, REQUEST_COLUMNS),
            page: pageParam(query),
        }),
        [query],
    );
    const { clientIp, host, target, outcome, entryId, sorting, page } = params;
    const filter = useMemo(
        () => ({ clientIp: clientIp || null, host: host || null, target: target || null, outcome, entryId }),
        [clientIp, host, target, outcome, entryId],
    );
    const links = useMemo(
        () => ({ ClientIp: clientIp, Host: host, Target: target, Outcome: outcome, EntryId: entryId }),
        [clientIp, host, target, outcome, entryId],
    );
    return (
        <>
            <RequestFilters
                path='/requests'
                clientIp={clientIp}
                clientIpLocked={false}
                host={host}
                target={target}
                outcome={outcome}
                entryId={entryId}
            />
            {entryId !== null && (
                <p className='notice'>
                    {'Only requests answered with '}
                    <Link to={`/entries/${entryId}`}>{`entry ${entryId}`}</Link>.
                </p>
            )}
            <RequestsPanel
                path='/requests'
                filter={filter}
                links={links}
                sorting={sorting}
                page={page}
            />
        </>
    );
};

export default React.memo(RequestsPage);
