import { useTitle } from '../../hooks/useTitle';
import { useWatch } from '../../hooks/useWatch';
import { useWatchPresence } from '../../hooks/useWatched';
import Presence from '../Common/Presence';
import GeneratorsOverview from './GeneratorsOverview';
import React from 'react';

const GeneratorsPage: React.FC = () => {
    useTitle('Generators');
    const watch = useWatch({ view: 'generators' });
    const presence = useWatchPresence(watch);
    if (presence !== 'present') return <Presence presence={presence} />;
    return <GeneratorsOverview watch={watch} />;
};

export default React.memo(GeneratorsPage);
