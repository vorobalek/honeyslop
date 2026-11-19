import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { ClientType } from '../../types/clientType';
import CountryFlag from '../Common/CountryFlag';
import React from 'react';

type ClientHeadingProps = {
    watch: Watch<ClientType>;
};

const ClientHeading: React.FC<ClientHeadingProps> = ({ watch }) => {
    const ip = useWatched(watch, (client) => client.ip);
    const country = useWatched(watch, (client) => client.country);
    return (
        <h1 className='request-line'>
            {country && <CountryFlag country={country} />}
            <span className='mono'>{ip}</span>
        </h1>
    );
};

export default React.memo(ClientHeading);
