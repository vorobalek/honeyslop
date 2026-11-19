import { useTitle } from '../../hooks/useTitle';
import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { RequestDetailsType } from '../../types/requestDetailsType';
import Link from '../Common/Link';
import RequestHeading from './RequestHeading';
import RequestMeta from './RequestMeta';
import React from 'react';

type RequestDetailsProps = {
    watch: Watch<RequestDetailsType>;
};

const RequestDetails: React.FC<RequestDetailsProps> = ({ watch }) => {
    const record = useWatched(watch, (view) => view.request);
    const country = useWatched(watch, (view) => view.country);
    useTitle(`${record.method} ${record.host}${record.target}`);
    return (
        <>
            <p className='back'>
                <Link to='/requests'>← All requests</Link>
                {' · '}
                <Link
                    to={`/clients/${encodeURIComponent(record.clientIp)}`}
                >{`everything from ${record.clientIp}`}</Link>
            </p>
            <RequestHeading record={record} />
            <RequestMeta
                record={record}
                country={country}
            />
            <h2>Request</h2>
            <pre className='code'>{`${record.method} ${record.target}\n${record.headers ?? ''}\n${record.body ?? ''}`}</pre>
        </>
    );
};

export default React.memo(RequestDetails);
