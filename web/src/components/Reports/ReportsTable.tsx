import { useSession } from '../../hooks/useSession';
import { useWatched } from '../../hooks/useWatched';
import { directionOf } from '../../lib/listQuery';
import { Watch } from '../../lib/watchStore';
import { ReportsViewType } from '../../types/reportsViewType';
import { SortingType } from '../../types/sortingType';
import SortHeader from '../Common/SortHeader';
import NoReportsRow from './NoReportsRow';
import ReportsTableRow from './ReportsTableRow';
import React from 'react';

type ReportsTableProps = {
    watch: Watch<ReportsViewType>;
    sorting: SortingType<string> | null;
};

const NO_FILTER = {};

const ReportsTable: React.FC<ReportsTableProps> = ({ watch, sorting }) => {
    const { canOperate } = useSession();
    const items = useWatched(watch, (view) => view.reports.items);
    const countries = useWatched(watch, (view) => view.countries);
    const header = (column: string, label: string, className?: string) => (
        <SortHeader
            path='/reports'
            filter={NO_FILTER}
            column={column}
            current={directionOf(sorting, column)}
            label={label}
            className={className}
        />
    );
    return (
        <div className='table-scroll'>
            <table className='entries'>
                <thead>
                    <tr>
                        {header('Requested', 'Requested')}
                        {header('Client', 'Client')}
                        {canOperate && header('By', 'By')}
                        {header('To', 'To')}
                        {header('Status', 'Status')}
                        {header('Sent', 'Sent')}
                        {header('Requests', 'Requests', 'number')}
                        {header('Attempts', 'Attempts', 'number')}
                        {header('Error', 'Error')}
                    </tr>
                </thead>
                <tbody>
                    {items.map((report) => (
                        <ReportsTableRow
                            key={report.id}
                            report={report}
                            showClient
                            country={countries[report.clientIp]}
                            showRequester={canOperate}
                        />
                    ))}
                    {items.length === 0 && <NoReportsRow columns={canOperate ? 8 : 7} />}
                </tbody>
            </table>
        </div>
    );
};

export default React.memo(ReportsTable);
