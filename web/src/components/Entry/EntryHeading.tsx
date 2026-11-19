import { useWatched } from '../../hooks/useWatched';
import { entryStateBadge } from '../../lib/badges';
import { Watch } from '../../lib/watchStore';
import { EntryViewType } from '../../types/entryViewType';
import React from 'react';

type EntryHeadingProps = {
    watch: Watch<EntryViewType>;
};

const EntryHeading: React.FC<EntryHeadingProps> = ({ watch }) => {
    const state = useWatched(watch, (view) => view.state);
    const method = useWatched(watch, (view) => view.entry.method);
    const host = useWatched(watch, (view) => view.entry.host);
    const target = useWatched(watch, (view) => view.entry.target);
    return (
        <h1 className='request-line'>
            <span className={entryStateBadge(state)}>{state.toLowerCase()}</span>
            <span className='mono'>{method}</span>
            <span className='mono host'>{host}</span>
            <span className='mono target'>{target}</span>
        </h1>
    );
};

export default React.memo(EntryHeading);
