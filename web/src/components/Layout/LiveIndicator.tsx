import { useLiveStatus } from '../../hooks/useLiveStatus';
import React from 'react';

const LABELS = { connecting: 'connecting…', online: 'live', reconnecting: 'reconnecting…' } as const;

const LiveIndicator: React.FC = () => {
    const status = useLiveStatus();
    return (
        <span
            className={status === 'online' ? 'live-status online' : 'live-status'}
            data-live-status=''
        >
            {LABELS[status]}
        </span>
    );
};

export default React.memo(LiveIndicator);
