import { CountryType } from '../../types/countryType';
import CountryFlag from './CountryFlag';
import Link from './Link';
import React from 'react';

type ClientIpProps = {
    ip: string;
    country: CountryType | null | undefined;
    delay?: number;
    learned?: boolean;
};

const ClientIp: React.FC<ClientIpProps> = ({ ip, country, delay, learned }) => (
    <>
        <Link
            className='client'
            to={`/clients/${encodeURIComponent(ip)}`}
        >
            {country && <CountryFlag country={country} />}
            {ip}
        </Link>
        {delay !== undefined && <span className='hint client-delay'>{` (+${delay} ms${learned ? ' auto' : ''})`}</span>}
    </>
);

export default React.memo(ClientIp);
