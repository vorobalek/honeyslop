import { WhoisType } from '../../types/whoisType';
import React from 'react';

type WhoisOriginsProps = {
    whois: WhoisType;
};

const WhoisOrigins: React.FC<WhoisOriginsProps> = ({ whois }) => (
    <>
        <span className='mono'>{whois.prefix}</span>
        {whois.origins.map((origin) => (
            <React.Fragment key={origin.asn}>
                {' · '}
                <a
                    href={`https://bgp.he.net/AS${origin.asn}`}
                    target='_blank'
                    rel='noopener'
                >
                    {`AS${origin.asn}`}
                </a>
                {` ${origin.holder}`}
            </React.Fragment>
        ))}
    </>
);

export default React.memo(WhoisOrigins);
