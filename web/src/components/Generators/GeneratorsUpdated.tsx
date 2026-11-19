import { useWatched } from '../../hooks/useWatched';
import { clock } from '../../lib/time';
import { Watch } from '../../lib/watchStore';
import { GeneratorsViewType } from '../../types/generatorsViewType';
import React from 'react';

type GeneratorsUpdatedProps = {
    watch: Watch<GeneratorsViewType>;
};

const GeneratorsUpdated: React.FC<GeneratorsUpdatedProps> = ({ watch }) => {
    const asOf = useWatched(watch, (view) => view.asOf);
    return (
        <span className='count'>{`live, updated ${clock(asOf)} UTC · production figures cover the last 24 hours`}</span>
    );
};

export default React.memo(GeneratorsUpdated);
