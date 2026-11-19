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

function delayText(manual: number | null, learned: number | null, timeout: number | null): string {
    if (manual !== null) return `${manual} ms, set on this address`;
    if (learned === null) return 'none; the delays of the entries apply';
    return timeout === null
        ? `${learned} ms, learned; it has not dropped a connection yet`
        : `${learned} ms, learned; it drops the connection at about ${timeout} ms`;
}

const ClientMeta: React.FC<ClientMetaProps> = ({ watch }) => {
    const country = useWatched(watch, (client) => client.country);
    const activity = useWatched(watch, (client) => client.activity);
    const ip = useWatched(watch, (client) => client.ip);
    const delay = useWatched(watch, (client) => client.responseDelayMilliseconds);
    const learned = useWatched(watch, (client) => client.learnedDelayMilliseconds);
    const timeout = useWatched(watch, (client) => client.learnedTimeoutMilliseconds);
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
                    {delayText(delay, learned, timeout)}{' '}
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
