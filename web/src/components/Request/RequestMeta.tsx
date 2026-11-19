import { display } from '../../lib/time';
import { CountryType } from '../../types/countryType';
import { RequestRecordType } from '../../types/requestRecordType';
import ClientIp from '../Common/ClientIp';
import Link from '../Common/Link';
import React from 'react';

type RequestMetaProps = {
    record: RequestRecordType;
    country: CountryType | null;
};

const RequestMeta: React.FC<RequestMetaProps> = ({ record, country }) => (
    <dl className='meta'>
        <dt>Received</dt>
        <dd>{`${display(record.receivedAt)} UTC`}</dd>
        <dt>Client</dt>
        <dd className='mono'>
            <ClientIp
                ip={record.clientIp}
                country={country}
            />
        </dd>
        <dt>Response</dt>
        <dd className='mono'>{`${record.responseStatus} in ${record.durationMs}ms`}</dd>
        <dt>Cache entry</dt>
        <dd>
            {record.entryId !== null ? <Link to={`/entries/${record.entryId}`}>{`entry ${record.entryId}`}</Link> : '—'}
        </dd>
        <dt>Answered by</dt>
        <dd className='mono'>{record.instance ?? '—'}</dd>
    </dl>
);

export default React.memo(RequestMeta);
