import { useWatchLater } from '../../hooks/useWatchLater';
import { useWatchPresence, useWatchRevision } from '../../hooks/useWatched';
import { refreshWatch } from '../../lib/watchStore';
import { ClientAbuseType } from '../../types/clientAbuseType';
import WhoisSection from './WhoisSection';
import React, { useCallback, useEffect, useState } from 'react';

type WhoisPanelProps = {
    ip: string;
};

const WhoisPanel: React.FC<WhoisPanelProps> = ({ ip }) => {
    const [watch, loaded] = useWatchLater<ClientAbuseType>('watchClientAbuse', ip);
    const presence = useWatchPresence(watch);
    const revision = useWatchRevision(watch);
    const [retrying, setRetrying] = useState(false);
    const retry = useCallback(() => {
        setRetrying(true);
        refreshWatch(watch);
    }, [watch]);
    useEffect(() => setRetrying(false), [revision]);

    if (!loaded) return <p className='hint'>Looking it up in RIPEstat…</p>;
    if (retrying) return <p className='hint'>Looking it up again…</p>;
    if (presence !== 'present') return null;
    return (
        <WhoisSection
            ip={ip}
            watch={watch}
            onRetry={retry}
        />
    );
};

export default React.memo(WhoisPanel);
