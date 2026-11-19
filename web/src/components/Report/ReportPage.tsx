import { useWatch } from '../../hooks/useWatch';
import { useWatchPresence } from '../../hooks/useWatched';
import { ReportDetailsType } from '../../types/reportDetailsType';
import Presence from '../Common/Presence';
import ReportDetails from './ReportDetails';
import React from 'react';

type ReportPageProps = {
    id: number;
};

const ReportPage: React.FC<ReportPageProps> = ({ id }) => {
    const watch = useWatch<ReportDetailsType>('watchReport', id);
    const presence = useWatchPresence(watch);
    if (presence !== 'present') return <Presence presence={presence} />;
    return <ReportDetails watch={watch} />;
};

export default React.memo(ReportPage);
