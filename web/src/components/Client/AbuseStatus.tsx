import { countOf } from '../../lib/numbers';
import { display, displayOrDash } from '../../lib/time';
import { ReportType } from '../../types/reportType';
import Link from '../Common/Link';
import React from 'react';

type AbuseStatusProps = {
    lastReport: ReportType | null;
    lastSent: ReportType | null;
    newSinceLastSent: number;
    showRequester: boolean;
};

const PENDING = new Set(['Queued', 'Sending', 'Retrying']);

const AbuseStatus: React.FC<AbuseStatusProps> = ({ lastReport, lastSent, newSinceLastSent, showRequester }) => {
    const by = (name: string | null) => (showRequester ? ` by ${name}` : '');
    return (
        <>
            {lastReport && PENDING.has(lastReport.state) && (
                <p className='notice'>
                    {lastReport.state === 'Sending' ? (
                        <>
                            {'Sending to '}
                            <span className='mono'>{lastReport.recipients.join(', ')}</span>
                            {' right now.'}
                        </>
                    ) : lastReport.retryAt ? (
                        <>
                            {`The last attempt failed, the next one is at ${display(lastReport.retryAt)} UTC: `}
                            <span className='error'>{lastReport.error}</span>
                        </>
                    ) : (
                        <>
                            {'Queued for '}
                            <span className='mono'>{lastReport.recipients.join(', ')}</span>
                            {` at ${display(lastReport.createdAt)} UTC${by(lastReport.requestedBy)}.`}
                        </>
                    )}
                </p>
            )}
            {lastReport && lastReport.state === 'Failed' && (
                <p className='error'>
                    {'The report to '}
                    <span className='mono'>{lastReport.recipients.join(', ')}</span>
                    {` failed: ${lastReport.error}`}
                </p>
            )}
            {lastSent && (
                <p className='notice'>
                    {'Sent to '}
                    <span className='mono'>{lastSent.recipients.join(', ')}</span>
                    {` at ${displayOrDash(lastSent.sentAt)} UTC${by(lastSent.requestedBy)}, with ${countOf(lastSent.attachedRequests ?? 0, 'request')} attached. `}
                    <Link to={`/reports/${lastSent.id}`}>Read the letter</Link>
                    {newSinceLastSent > 0
                        ? `. ${countOf(newSinceLastSent, 'new request')} arrived since then.`
                        : '. Nothing new from this address since then.'}
                </p>
            )}
        </>
    );
};

export default React.memo(AbuseStatus);
