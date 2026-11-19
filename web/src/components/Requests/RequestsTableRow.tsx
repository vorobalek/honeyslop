import { outcomeBadge } from '../../lib/badges';
import { display } from '../../lib/time';
import { CountryType } from '../../types/countryType';
import { RequestRowType } from '../../types/requestRowType';
import ClientIp from '../Common/ClientIp';
import Link from '../Common/Link';
import React from 'react';

type RequestsTableRowProps = {
    request: RequestRowType;
    country: CountryType | undefined;
};

const RequestsTableRow: React.FC<RequestsTableRowProps> = ({ request, country }) => (
    <tr>
        <td className='time'>
            <Link to={`/requests/${request.id}`}>{display(request.receivedAt)}</Link>
        </td>
        <td className='mono'>
            <ClientIp
                ip={request.clientIp}
                country={country}
            />
        </td>
        <td className='mono'>{request.method}</td>
        <td className='mono host'>{request.host}</td>
        <td className='mono target'>
            {request.entryId !== null ? (
                <Link to={`/entries/${request.entryId}`}>{request.target}</Link>
            ) : (
                request.target
            )}
        </td>
        <td>
            <span className={outcomeBadge(request.outcome)}>{request.outcome.toLowerCase()}</span>
        </td>
        <td className='number mono'>{request.responseStatus}</td>
        <td className='number'>{request.durationMs}</td>
        <td className='agent'>{request.userAgent}</td>
    </tr>
);

export default React.memo(RequestsTableRow);
