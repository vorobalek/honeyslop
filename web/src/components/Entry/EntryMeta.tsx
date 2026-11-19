import { useWatched } from '../../hooks/useWatched';
import { oneDecimal } from '../../lib/numbers';
import { display, displayOrDash } from '../../lib/time';
import { Watch } from '../../lib/watchStore';
import { EntryViewType } from '../../types/entryViewType';
import ClientIp from '../Common/ClientIp';
import EntryDelayButton from './EntryDelayButton';
import React from 'react';

type EntryMetaProps = {
    watch: Watch<EntryViewType>;
};

const EntryMeta: React.FC<EntryMetaProps> = ({ watch }) => {
    const entry = useWatched(watch, (view) => view.entry);
    const country = useWatched(watch, (view) => view.country);
    const defaultDelay = useWatched(watch, (view) => view.defaultResponseDelayMilliseconds);
    const expired = useWatched(watch, (view) => view.state === 'Expired');
    const generation = entry.generationMs !== null ? `${oneDecimal(entry.generationMs / 1000)} s` : '—';
    return (
        <dl className='meta'>
            <dt>Response</dt>
            <dd className='mono'>{entry.responseStatus ?? '—'}</dd>
            <dt>Hits</dt>
            <dd>{entry.hits}</dd>
            <dt>Created</dt>
            <dd>{display(entry.createdAt)}</dd>
            <dt>Generated</dt>
            <dd>{displayOrDash(entry.generatedAt)}</dd>
            <dt>Last hit</dt>
            <dd>{displayOrDash(entry.lastHitAt)}</dd>
            <dt>Generation</dt>
            <dd>{`${generation}, ${entry.completionTokens ?? '—'} tokens, ${entry.model ?? '—'}`}</dd>
            <dt>Generator</dt>
            <dd className='mono'>{entry.generator ?? '—'}</dd>
            <dt>Delay</dt>
            <dd>
                {entry.responseDelayMilliseconds !== null
                    ? `${entry.responseDelayMilliseconds}ms, set on this entry`
                    : `${defaultDelay}ms, the default`}
                {!expired && (
                    <>
                        {' '}
                        <EntryDelayButton watch={watch} />
                    </>
                )}
            </dd>
            <dt>Attempts</dt>
            <dd>{entry.attempts}</dd>
            <dt>Claimed by</dt>
            <dd className='mono'>{`${entry.claimedBy ?? '—'} at ${displayOrDash(entry.claimedAt)}`}</dd>
            <dt>First client</dt>
            <dd className='mono'>
                {entry.clientIp ? (
                    <ClientIp
                        ip={entry.clientIp}
                        country={country}
                    />
                ) : (
                    '—'
                )}
            </dd>
            {entry.changedBy && (
                <>
                    <dt>Changed by</dt>
                    <dd>{entry.changedBy}</dd>
                </>
            )}
            {entry.guidance && (
                <>
                    <dt>Comment</dt>
                    <dd>{entry.guidance}</dd>
                </>
            )}
            {entry.error && (
                <>
                    <dt>Error</dt>
                    <dd className='error'>{entry.error}</dd>
                </>
            )}
        </dl>
    );
};

export default React.memo(EntryMeta);
