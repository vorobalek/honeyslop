import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { EntryViewType } from '../../types/entryViewType';
import EntryChangeButton from './EntryChangeButton';
import EntryDelayButton from './EntryDelayButton';
import EntryExpireForm from './EntryExpireForm';
import React from 'react';

type EntryActionsProps = {
    id: number;
    watch: Watch<EntryViewType>;
};

const EntryActions: React.FC<EntryActionsProps> = ({ id, watch }) => {
    const expired = useWatched(watch, (view) => view.state === 'Expired');
    const replaced = useWatched(watch, (view) => view.currentId !== null);
    if (expired && replaced) return null;
    return (
        <div className='actions'>
            <EntryChangeButton watch={watch} />
            {!expired && <EntryDelayButton watch={watch} />}
            {!expired && <EntryExpireForm id={id} />}
        </div>
    );
};

export default React.memo(EntryActions);
