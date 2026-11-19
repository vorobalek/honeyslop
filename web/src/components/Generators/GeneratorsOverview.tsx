import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { GeneratorsViewType } from '../../types/generatorsViewType';
import GeneratingTable from './GeneratingTable';
import GeneratorsStats from './GeneratorsStats';
import GeneratorsTable from './GeneratorsTable';
import GeneratorsUpdated from './GeneratorsUpdated';
import QueueHeading from './QueueHeading';
import QueueTable from './QueueTable';
import React from 'react';

type GeneratorsOverviewProps = {
    watch: Watch<GeneratorsViewType>;
};

const GeneratorsOverview: React.FC<GeneratorsOverviewProps> = ({ watch }) => {
    const configured = useWatched(watch, (view) => view.configured);
    return (
        <>
            {!configured && (
                <p className='notice'>
                    LLM_URL is not set for the admin, so model servers cannot be discovered. Queue figures come from the
                    database.
                </p>
            )}
            <div>
                <GeneratorsStats watch={watch} />
                <div className='actions'>
                    <GeneratorsUpdated watch={watch} />
                </div>
                <GeneratorsTable watch={watch} />
                <h2>Generating now</h2>
                <GeneratingTable watch={watch} />
                <QueueHeading watch={watch} />
                <p className='hint'>
                    Entries somebody asked for within the wait window go first, newest first; the rest are taken oldest
                    first.
                </p>
                <QueueTable watch={watch} />
            </div>
        </>
    );
};

export default React.memo(GeneratorsOverview);
