import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { ClientsViewType } from '../../types/clientsViewType';
import React from 'react';

type ClientsCountProps = {
    watch: Watch<ClientsViewType>;
};

const ClientsCount: React.FC<ClientsCountProps> = ({ watch }) => {
    const total = useWatched(watch, (view) => view.clients.total);
    return <span className='count'>{`${total} clients, most recent first`}</span>;
};

export default React.memo(ClientsCount);
