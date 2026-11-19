import { useWatched } from '../../hooks/useWatched';
import { countOf } from '../../lib/numbers';
import { display } from '../../lib/time';
import { Watch } from '../../lib/watchStore';
import { ClientType } from '../../types/clientType';
import ClientDelayButton from './ClientDelayButton';
import React from 'react';

type ClientMetaProps = {
    watch: Watch<ClientType>;
};

const ClientMeta: React.FC<ClientMetaProps> = ({ watch }) => {
    const country = useWatched(watch, (client) => client.country);
    const activity = useWatched(watch, (client) => client.activity);
    const ip = useWatched(watch, (client) => client.ip);
    const delay = useWatched(watch, (client) => client.responseDelayMilliseconds);
    return (
        <>
            <dl className='meta'>
                <dt>Country</dt>
                <dd>{country ? `${country.name} (${country.code})` : 'unknown'}</dd>
                {activity && (
                    <>
                        <dt>First request</dt>
                        <dd>{`${display(activity.firstSeenAt)} UTC`}</dd>
                        <dt>Last request</dt>
                        <dd>{`${display(activity.lastSeenAt)} UTC`}</dd>
                        <dt>Requests</dt>
                        <dd>
                            {`${countOf(activity.requests, 'request')} for ${countOf(activity.targets, 'path')} on ${countOf(activity.hosts, 'host')}`}
                        </dd>
                        <dt>Last User-Agent</dt>
                        <dd className='agent'>{activity.lastUserAgent ?? '—'}</dd>
                    </>
                )}
                <dt>Delay</dt>
                <dd>
                    {delay !== null ? `${delay} ms, set on this address` : 'none; the delays of the entries apply'}{' '}
                    <ClientDelayButton
                        ip={ip}
                        milliseconds={delay}
                    />
                </dd>
            </dl>
            {!activity && (
                <p className='notice'>
                    The request history no longer has anything from this address: it is known only as the first client
                    of a cached entry.
                </p>
            )}
        </>
    );
};

export default React.memo(ClientMeta);
