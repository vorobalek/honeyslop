import { SortableColumn, useClientSort } from '../../hooks/useClientSort';
import { display, displayOrDash } from '../../lib/time';
import { ReportType } from '../../types/reportType';
import ClientSortHeader from '../Common/ClientSortHeader';
import NoReportsRow from '../Reports/NoReportsRow';
import ReportsTableRow from '../Reports/ReportsTableRow';
import React, { useMemo } from 'react';

type ClientReportsTableProps = {
    reports: readonly ReportType[];
    showRequester: boolean;
};

const REQUESTER: SortableColumn<ReportType> = { label: 'By', value: (report) => report.requestedBy ?? '' };

const COLUMNS: readonly SortableColumn<ReportType>[] = [
    { label: 'Requested', value: (report) => display(report.createdAt) },
    { label: 'To', value: (report) => report.recipients.join(', ') },
    { label: 'Status', value: (report) => report.state.toLowerCase() },
    { label: 'Sent', value: (report) => displayOrDash(report.sentAt) },
    {
        label: 'Requests',
        className: 'number',
        value: (report) => (report.attachedRequests === null ? '—' : String(report.attachedRequests)),
    },
    { label: 'Attempts', className: 'number', value: (report) => String(report.attempts) },
    { label: 'Error', value: (report) => report.error ?? '' },
];

const ClientReportsTable: React.FC<ClientReportsTableProps> = ({ reports, showRequester }) => {
    const columns = useMemo(
        () => (showRequester ? [COLUMNS[0], REQUESTER, ...COLUMNS.slice(1)] : COLUMNS),
        [showRequester],
    );
    const [sorted, state, toggle] = useClientSort(reports, columns, undefined);
    return (
        <div className='table-scroll'>
            <table
                className='entries'
                data-sortable='reports'
            >
                <thead>
                    <ClientSortHeader
                        columns={columns}
                        state={state}
                        onToggle={toggle}
                    />
                </thead>
                <tbody>
                    {sorted.map((report) => (
                        <ReportsTableRow
                            key={report.id}
                            report={report}
                            showClient={false}
                            showRequester={showRequester}
                        />
                    ))}
                    {reports.length === 0 && <NoReportsRow columns={showRequester ? 7 : 6} />}
                </tbody>
            </table>
        </div>
    );
};

export default React.memo(ClientReportsTable);
