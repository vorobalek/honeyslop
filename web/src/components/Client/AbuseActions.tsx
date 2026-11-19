import { AbuseDraftType } from '../../types/abuseDraftType';
import SendReportForm from './SendReportForm';
import React from 'react';

type AbuseActionsProps = {
    ip: string;
    draft: AbuseDraftType;
    recipients: readonly string[];
    blocked: readonly string[];
    reportPending: boolean;
    alreadyReported: boolean;
    followUp: boolean;
    newSinceLastSent: number;
};

const AbuseActions: React.FC<AbuseActionsProps> = ({
    ip,
    draft,
    recipients,
    blocked,
    reportPending,
    alreadyReported,
    followUp,
    newSinceLastSent,
}) => {
    let content: React.ReactNode;
    if (recipients.length === 0 && blocked.length > 0)
        content = (
            <span className='hint'>{`Every abuse contact of this network is blocked: ${blocked.join(', ')}.`}</span>
        );
    else if (recipients.length === 0)
        content = <span className='hint'>The registry publishes no abuse contact for this network.</span>;
    else if (reportPending) content = <span className='hint'>A report about this address is already on its way.</span>;
    else if (alreadyReported) {
        content = (
            <span className='hint'>
                Nothing new to report until this address sends more. The letter and the archive below are there to send
                it by hand.
            </span>
        );
    } else {
        content = (
            <SendReportForm
                ip={ip}
                draft={draft}
                recipients={recipients}
                followUp={followUp}
                newSinceLastSent={newSinceLastSent}
            />
        );
    }
    return <div className='actions'>{content}</div>;
};

export default React.memo(AbuseActions);
