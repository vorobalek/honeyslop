import { useWatch } from '../../hooks/useWatch';
import { useWatchPresence } from '../../hooks/useWatched';
import { RequestDetailsType } from '../../types/requestDetailsType';
import Presence from '../Common/Presence';
import RequestDetails from './RequestDetails';
import React from 'react';

type RequestPageProps = {
    id: number;
};

const RequestPage: React.FC<RequestPageProps> = ({ id }) => {
    const watch = useWatch<RequestDetailsType>('watchRequest', id);
    const presence = useWatchPresence(watch);
    if (presence !== 'present') return <Presence presence={presence} />;
    return <RequestDetails watch={watch} />;
};

export default React.memo(RequestPage);
