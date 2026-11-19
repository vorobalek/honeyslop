import { display } from '../../lib/time';
import { WhoisType } from '../../types/whoisType';
import AbuseContacts from './AbuseContacts';
import WhoisOrigins from './WhoisOrigins';
import React from 'react';

type WhoisDetailsProps = {
    ip: string;
    whois: WhoisType;
    recipients: readonly string[];
    blocked: readonly string[];
};

const WhoisDetails: React.FC<WhoisDetailsProps> = ({ ip, whois, recipients, blocked }) => (
    <>
        <dl className='meta'>
            <dt>Routed as</dt>
            <dd>{whois.prefix === null ? 'not announced' : <WhoisOrigins whois={whois} />}</dd>
            <dt>Registered network</dt>
            <dd>
                <span className='mono'>{whois.network ?? '—'}</span>
                {whois.networkName === null ? '' : ` · ${whois.networkName}`}
            </dd>
            <dt>Owner</dt>
            <dd>{`${whois.owner ?? '—'}${whois.country === null ? '' : ` · ${whois.country}`}`}</dd>
            <dt>Abuse contact</dt>
            <dd>
                {recipients.length + blocked.length === 0 ? (
                    'none published'
                ) : (
                    <AbuseContacts
                        recipients={recipients}
                        blocked={blocked}
                    />
                )}
            </dd>
            <dt>Source</dt>
            <dd>
                {`${whois.registry ?? 'unknown registry'} via `}
                <a
                    href={`https://stat.ripe.net/resource/${ip}`}
                    target='_blank'
                    rel='noopener'
                >
                    RIPEstat
                </a>
                {`, looked up ${display(whois.lookedUpAt)} UTC`}
            </dd>
        </dl>
        {whois.records.length > 0 && (
            <details id='registry-records'>
                <summary>Registry records</summary>
                <pre className='code'>{whois.records}</pre>
            </details>
        )}
    </>
);

export default React.memo(WhoisDetails);
