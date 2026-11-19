import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { ReportsViewType } from '../../types/reportsViewType';
import React from 'react';

type ReportsCountProps = {
    watch: Watch<ReportsViewType>;
};

const ReportsCount: React.FC<ReportsCountProps> = ({ watch }) => {
    const total = useWatched(watch, (view) => view.reports.total);
    return <span className='count'>{`${total} abuse reports, newest first`}</span>;
};

export default React.memo(ReportsCount);
