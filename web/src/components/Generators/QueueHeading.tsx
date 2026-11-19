import { useWatched } from '../../hooks/useWatched';
import { duration, secondsBetween } from '../../lib/time';
import { Watch } from '../../lib/watchStore';
import { GeneratorsViewType } from '../../types/generatorsViewType';
import React from 'react';

type QueueHeadingProps = {
    watch: Watch<GeneratorsViewType>;
};

const QueueHeading: React.FC<QueueHeadingProps> = ({ watch }) => {
    const text = useWatched(watch, (view) => {
        const oldest = view.oldestQueuedAt
            ? `, the oldest for ${duration(secondsBetween(view.oldestQueuedAt, view.asOf))}`
            : '';
        return `Queue · ${view.totals.queued} waiting${oldest}`;
    });
    return <h2>{text}</h2>;
};

export default React.memo(QueueHeading);
