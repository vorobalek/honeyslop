import { useQuery } from '../../hooks/useLocation';
import { useTitle } from '../../hooks/useTitle';
import { useWatch } from '../../hooks/useWatch';
import { useWatchPresence } from '../../hooks/useWatched';
import { REPORT_COLUMNS } from '../../lib/constants';
import { pageParam, sortingParam } from '../../lib/listQuery';
import { ReportsViewType } from '../../types/reportsViewType';
import ListPager from '../Common/ListPager';
import Presence from '../Common/Presence';
import ReportsCount from './ReportsCount';
import ReportsTable from './ReportsTable';
import React, { useMemo } from 'react';

const NO_FILTER = {};
const selectReports = (view: ReportsViewType) => view.reports;

const ReportsPage: React.FC = () => {
    useTitle('Reports');
    const query = useQuery();
    const sorting = useMemo(() => sortingParam(query, REPORT_COLUMNS), [query]);
    const watch = useWatch<ReportsViewType>('watchReports', { sort: sorting, page: pageParam(query) });
    const presence = useWatchPresence(watch);
    if (presence !== 'present') return <Presence presence={presence} />;
    return (
        <div>
            <div className='actions'>
                <ReportsCount watch={watch} />
            </div>
            <ReportsTable
                watch={watch}
                sorting={sorting}
            />
            <ListPager
                watch={watch}
                select={selectReports}
                path='/reports'
                filter={NO_FILTER}
                sorting={sorting}
                newer='← Newer'
            />
        </div>
    );
};

export default React.memo(ReportsPage);
