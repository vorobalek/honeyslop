import { WatchPresence } from '../../hooks/useWatched';
import LoadFailed from './LoadFailed';
import NotFound from './NotFound';
import React from 'react';

type PresenceProps = {
    presence: WatchPresence;
};

const Presence: React.FC<PresenceProps> = ({ presence }) => (presence === 'failed' ? <LoadFailed /> : <NotFound />);

export default React.memo(Presence);
