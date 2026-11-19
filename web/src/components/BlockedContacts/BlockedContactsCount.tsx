import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { BlockedContactsViewType } from '../../types/blockedContactsViewType';
import React from 'react';

type BlockedContactsCountProps = {
    watch: Watch<BlockedContactsViewType>;
};

const BlockedContactsCount: React.FC<BlockedContactsCountProps> = ({ watch }) => {
    const total = useWatched(watch, (view) => view.contacts.total);
    return <span className='count'>{`${total} blocked`}</span>;
};

export default React.memo(BlockedContactsCount);
