import { useTitle } from '../../hooks/useTitle';
import { useWatched } from '../../hooks/useWatched';
import { reportStateBadge } from '../../lib/badges';
import { Watch } from '../../lib/watchStore';
import { ReportDetailsType } from '../../types/reportDetailsType';
import Link from '../Common/Link';
import ReportMeta from './ReportMeta';
import React from 'react';

type ReportDetailsProps = {
    watch: Watch<ReportDetailsType>;
};

const ReportDetails: React.FC<ReportDetailsProps> = ({ watch }) => {
    const details = useWatched(watch, (view) => view);
    const { report } = details;
    useTitle(report.subject);
    return (
        <>
            <p className='back'>
                <Link to='/reports'>← All reports</Link>
                {' · '}
                <Link
                    to={`/clients/${encodeURIComponent(report.clientIp)}`}
                >{`everything about ${report.clientIp}`}</Link>
            </p>
            <h1 className='request-line'>
                <span className={reportStateBadge(report.state)}>{report.state.toLowerCase()}</span>
                <span>{report.subject}</span>
            </h1>
            <ReportMeta details={details} />
            <h2>Letter</h2>
            <pre className='code'>{details.body}</pre>
        </>
    );
};

export default React.memo(ReportDetails);
