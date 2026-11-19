import { useWatched } from '../../hooks/useWatched';
import { clock } from '../../lib/time';
import { Watch } from '../../lib/watchStore';
import { RequestsViewType } from '../../types/requestsViewType';
import React from 'react';

type RequestsCountProps = {
    watch: Watch<RequestsViewType>;
};

const RequestsCount: React.FC<RequestsCountProps> = ({ watch }) => {
    const total = useWatched(watch, (view) => view.requests.total);
    const asOf = useWatched(watch, (view) => view.asOf);
    return <span className='count'>{`${total} requests · live, updated ${clock(asOf)} UTC`}</span>;
};

export default React.memo(RequestsCount);
