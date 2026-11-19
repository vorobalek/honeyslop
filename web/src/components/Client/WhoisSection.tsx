import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { ClientAbuseType } from '../../types/clientAbuseType';
import AbuseReport from './AbuseReport';
import WhoisDetails from './WhoisDetails';
import WhoisUnavailable from './WhoisUnavailable';
import React from 'react';

type WhoisSectionProps = {
    ip: string;
    watch: Watch<ClientAbuseType>;
    onRetry: () => void;
};

const WhoisSection: React.FC<WhoisSectionProps> = ({ ip, watch, onRetry }) => {
    const whois = useWatched(watch, (abuse) => abuse.whois);
    const recipients = useWatched(watch, (abuse) => abuse.recipients);
    return (
        <div>
            {whois === null ? (
                <WhoisUnavailable onRetry={onRetry} />
            ) : (
                <WhoisDetails
                    ip={ip}
                    whois={whois}
                    recipients={recipients}
                />
            )}
            <AbuseReport
                ip={ip}
                watch={watch}
            />
        </div>
    );
};

export default React.memo(WhoisSection);
