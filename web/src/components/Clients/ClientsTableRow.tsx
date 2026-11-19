import { display } from '../../lib/time';
import { ClientActivityType } from '../../types/clientActivityType';
import { CountryType } from '../../types/countryType';
import ClientIp from '../Common/ClientIp';
import React from 'react';

type ClientsTableRowProps = {
    client: ClientActivityType;
    country: CountryType | undefined;
    delay: number | undefined;
    learned: boolean;
};

const ClientsTableRow: React.FC<ClientsTableRowProps> = ({ client, country, delay, learned }) => (
    <tr>
        <td className='mono'>
            <ClientIp
                ip={client.clientIp}
                country={country}
                delay={delay}
                learned={learned}
            />
        </td>
        <td className='number'>{client.requests}</td>
        <td className='number'>{client.hosts}</td>
        <td className='number'>{client.targets}</td>
        <td className='time'>{display(client.firstSeenAt)}</td>
        <td className='time'>{display(client.lastSeenAt)}</td>
        <td className='agent'>{client.lastUserAgent}</td>
    </tr>
);

export default React.memo(ClientsTableRow);
