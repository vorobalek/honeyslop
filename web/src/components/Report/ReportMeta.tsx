import { useSession } from '../../hooks/useSession';
import { countOf } from '../../lib/numbers';
import { display } from '../../lib/time';
import { ReportDetailsType } from '../../types/reportDetailsType';
import ClientIp from '../Common/ClientIp';
import ReportBounces from './ReportBounces';
import React from 'react';

type ReportMetaProps = {
    details: ReportDetailsType;
};

const ReportMeta: React.FC<ReportMetaProps> = ({ details }) => {
    const { canOperate } = useSession();
    const { report } = details;
    const attachment =
        report.attachedRequests !== null
            ? `${countOf(report.attachedRequests, 'request')} from this address, zipped; built when sending and not kept`
            : '—';
    return (
        <dl className='meta'>
            <dt>About</dt>
            <dd className='mono'>
                <ClientIp
                    ip={report.clientIp}
                    country={details.country}
                />
            </dd>
            <dt>To</dt>
            <dd className='mono'>{report.recipients.join(', ')}</dd>
            {canOperate && (
                <>
                    <dt>From</dt>
                    {details.fromIsExpected ? (
                        <dd>
                            <span className='mono hint'>{details.from}</span>{' '}
                            <span className='hint'>· expected; fixed when the reporter sends it</span>
                        </dd>
                    ) : (
                        <dd className='mono'>{details.from}</dd>
                    )}
                </>
            )}
            <dt>Requested</dt>
            <dd>{`${display(report.createdAt)} UTC${canOperate ? ` by ${report.requestedBy}` : ''}`}</dd>
            <dt>Sent</dt>
            <dd>{report.sentAt ? `${display(report.sentAt)} UTC` : '—'}</dd>
            <dt>Attempts</dt>
            <dd>{report.attempts}</dd>
            <dt>Attachment</dt>
            <dd>{attachment}</dd>
            <dt>Message-ID</dt>
            <dd className='mono'>{details.messageId ?? '—'}</dd>
            {report.error && (
                <>
                    <dt>Error</dt>
                    <dd className='error'>{report.error}</dd>
                </>
            )}
            {details.bounces.length > 0 && (
                <>
                    <dt>Bounced</dt>
                    <dd>
                        <ReportBounces bounces={details.bounces} />
                    </dd>
                </>
            )}
        </dl>
    );
};

export default React.memo(ReportMeta);
