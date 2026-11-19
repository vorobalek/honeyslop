import { useWatched } from '../../hooks/useWatched';
import { display, duration, secondsBetween } from '../../lib/time';
import { Watch } from '../../lib/watchStore';
import { EntryType } from '../../types/entryType';
import { EntryViewType } from '../../types/entryViewType';
import Link from '../Common/Link';
import React from 'react';

type EntryStateNoticeProps = {
    watch: Watch<EntryViewType>;
};

function queuedText(entry: EntryType, asOf: string): string {
    const requested = entry.requestedAt
        ? `, last requested ${duration(secondsBetween(entry.requestedAt, asOf))} ago`
        : '';
    return `Queued for ${duration(secondsBetween(entry.createdAt, asOf))}${requested}. The page updates when a generator takes it.`;
}

function failedText(attempts: number): string {
    const retries = attempts >= 3 ? ', no more retries until it is expired' : ', it will be retried';
    return `Failed ${attempts} ${attempts === 1 ? 'time' : 'times'}${retries}`;
}

const EntryStateNotice: React.FC<EntryStateNoticeProps> = ({ watch }) => {
    const state = useWatched(watch, (view) => view.state);
    const entry = useWatched(watch, (view) => view.entry);
    const asOf = useWatched(watch, (view) => view.asOf);
    const currentId = useWatched(watch, (view) => view.currentId);
    switch (state) {
        case 'Queued':
            return <p className='notice'>{queuedText(entry, asOf)}</p>;
        case 'Generating': {
            const running = duration(secondsBetween(entry.claimedAt ?? asOf, asOf));
            const attempt = entry.attempts + 1;
            if (entry.generator === null) {
                return (
                    <p className='notice'>
                        {`Taken ${running} ago, attempt ${attempt}, waiting for a free generator slot. The page updates when it is done.`}
                    </p>
                );
            }
            return (
                <p className='notice'>
                    {'Generating on '}
                    <span className='mono'>{entry.generator}</span>
                    {` for ${running}, attempt ${attempt}. The output streams below, the page updates when it is done.`}
                </p>
            );
        }
        case 'Failed':
            return <p className='notice'>{failedText(entry.attempts)}</p>;
        case 'Expired':
            return (
                <p className='notice'>
                    {`Expired ${display(entry.updatedAt)} UTC. It is no longer served and stays here while recorded requests refer to it. `}
                    {currentId !== null && <Link to={`/entries/${currentId}`}>The current entry for this path →</Link>}
                </p>
            );
        default:
            return null;
    }
};

export default React.memo(EntryStateNotice);
