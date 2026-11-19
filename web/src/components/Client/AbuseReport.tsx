import { useSession } from '../../hooks/useSession';
import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { ClientAbuseType } from '../../types/clientAbuseType';
import AbuseActions from './AbuseActions';
import AbuseLetter from './AbuseLetter';
import AbuseStatus from './AbuseStatus';
import ClientReportsTable from './ClientReportsTable';
import React from 'react';

type AbuseReportProps = {
    ip: string;
    watch: Watch<ClientAbuseType>;
};

const AbuseReport: React.FC<AbuseReportProps> = ({ ip, watch }) => {
    const { canOperate } = useSession();
    const reports = useWatched(watch, (abuse) => abuse.reports);
    const lastReport = useWatched(watch, (abuse) => abuse.lastReport);
    const lastSent = useWatched(watch, (abuse) => abuse.lastSent);
    const newSinceLastSent = useWatched(watch, (abuse) => abuse.newSinceLastSent);
    const draft = useWatched(watch, (abuse) => abuse.draft);
    const recipients = useWatched(watch, (abuse) => abuse.recipients);
    const blocked = useWatched(watch, (abuse) => abuse.blockedRecipients);
    const reportPending = useWatched(watch, (abuse) => abuse.reportPending);
    const alreadyReported = useWatched(watch, (abuse) => abuse.alreadyReported);
    return (
        <>
            {(canOperate || reports.length > 0) && <h2>Abuse report</h2>}
            <AbuseStatus
                lastReport={lastReport}
                lastSent={lastSent}
                newSinceLastSent={newSinceLastSent}
                showRequester={canOperate}
            />
            {canOperate && draft && (
                <AbuseActions
                    ip={ip}
                    draft={draft}
                    recipients={recipients}
                    blocked={blocked}
                    reportPending={reportPending}
                    alreadyReported={alreadyReported}
                    followUp={lastSent !== null}
                    newSinceLastSent={newSinceLastSent}
                />
            )}
            {reports.length > 0 && (
                <ClientReportsTable
                    reports={reports}
                    showRequester={canOperate}
                />
            )}
            {canOperate && draft && (
                <AbuseLetter
                    ip={ip}
                    draft={draft}
                    recipients={recipients}
                />
            )}
        </>
    );
};

export default React.memo(AbuseReport);
