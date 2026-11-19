import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { EntriesViewType } from '../../types/entriesViewType';
import StatTile from '../Common/StatTile';
import React from 'react';

type EntriesStatsProps = {
    watch: Watch<EntriesViewType>;
};

const EntriesStats: React.FC<EntriesStatsProps> = ({ watch }) => {
    const totals = useWatched(watch, (view) => view.totals);
    return (
        <section className='stats'>
            <StatTile
                value={totals.total}
                label='entries'
            />
            <StatTile
                value={totals.ready}
                label='ready'
            />
            <StatTile
                value={totals.generating}
                label='generating'
            />
            <StatTile
                value={totals.queued}
                label='queued'
            />
            <StatTile
                value={totals.failed}
                label='failed'
            />
            <StatTile
                value={totals.expired}
                label='expired, kept for requests'
            />
            <StatTile
                value={totals.hits}
                label='served from cache'
            />
        </section>
    );
};

export default React.memo(EntriesStats);
