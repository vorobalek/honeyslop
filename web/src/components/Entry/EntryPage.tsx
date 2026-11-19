import { useWatch } from '../../hooks/useWatch';
import { useWatchPresence } from '../../hooks/useWatched';
import Presence from '../Common/Presence';
import EntryDetails from './EntryDetails';
import React from 'react';

type EntryPageProps = {
    id: number;
};

const EntryPage: React.FC<EntryPageProps> = ({ id }) => {
    const watch = useWatch({ view: 'entry', id });
    const presence = useWatchPresence(watch);
    if (presence !== 'present') return <Presence presence={presence} />;
    return (
        <EntryDetails
            id={id}
            watch={watch}
        />
    );
};

export default React.memo(EntryPage);
