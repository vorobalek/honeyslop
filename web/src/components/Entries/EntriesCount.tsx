import { useWatched } from '../../hooks/useWatched';
import { clock } from '../../lib/time';
import { Watch } from '../../lib/watchStore';
import { EntriesViewType } from '../../types/entriesViewType';
import React from 'react';

type EntriesCountProps = {
    watch: Watch<EntriesViewType>;
};

const EntriesCount: React.FC<EntriesCountProps> = ({ watch }) => {
    const total = useWatched(watch, (view) => view.entries.total);
    const asOf = useWatched(watch, (view) => view.asOf);
    return <span className='count'>{`${total} matching · live, updated ${clock(asOf)} UTC`}</span>;
};

export default React.memo(EntriesCount);
