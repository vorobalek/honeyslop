import { CountryType } from '../../types/countryType';
import CountryFlag from './CountryFlag';
import Link from './Link';
import React from 'react';

type ClientIpProps = {
    ip: string;
    country: CountryType | null | undefined;
};

const ClientIp: React.FC<ClientIpProps> = ({ ip, country }) => (
    <Link
        className='client'
        to={`/clients/${encodeURIComponent(ip)}`}
    >
        {country && <CountryFlag country={country} />}
        {ip}
    </Link>
);

export default React.memo(ClientIp);
