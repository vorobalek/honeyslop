import { reportStateBadge } from '../../lib/badges';
import { display, displayOrDash } from '../../lib/time';
import { CountryType } from '../../types/countryType';
import { ReportType } from '../../types/reportType';
import ClientIp from '../Common/ClientIp';
import Link from '../Common/Link';
import React from 'react';

type ReportsTableRowProps = {
    report: ReportType;
    showClient: boolean;
    country?: CountryType;
    showRequester: boolean;
};

const ReportsTableRow: React.FC<ReportsTableRowProps> = ({ report, showClient, country, showRequester }) => (
    <tr>
        <td className='time'>
            <Link to={`/reports/${report.id}`}>{display(report.createdAt)}</Link>
        </td>
        {showClient && (
            <td className='mono'>
                <ClientIp
                    ip={report.clientIp}
                    country={country}
                />
            </td>
        )}
        {showRequester && <td>{report.requestedBy}</td>}
        <td className='mono'>{report.recipients.join(', ')}</td>
        <td>
            <span className={reportStateBadge(report.state)}>{report.state.toLowerCase()}</span>
        </td>
        <td className='time'>{displayOrDash(report.sentAt)}</td>
        <td className='number'>{report.attachedRequests ?? '—'}</td>
        <td className='number'>{report.attempts}</td>
        <td className='error'>{report.error}</td>
    </tr>
);

export default React.memo(ReportsTableRow);
