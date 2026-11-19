import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { GeneratorsViewType } from '../../types/generatorsViewType';
import StatTile from '../Common/StatTile';
import React from 'react';

type GeneratorsStatsProps = {
    watch: Watch<GeneratorsViewType>;
};

const GeneratorsStats: React.FC<GeneratorsStatsProps> = ({ watch }) => {
    const up = useWatched(watch, (view) => view.generators.filter((generator) => generator.status !== null).length);
    const slots = useWatched(watch, (view) => `${view.busySlots} / ${view.totalSlots}`);
    const totals = useWatched(watch, (view) => view.totals);
    return (
        <section className='stats'>
            <StatTile
                value={up}
                label='generators up'
            />
            <StatTile
                value={slots}
                label='slots busy'
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
        </section>
    );
};

export default React.memo(GeneratorsStats);
