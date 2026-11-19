import { Watch } from '../../lib/watchStore';
import { EntryViewType } from '../../types/entryViewType';
import EntryChangeButton from './EntryChangeButton';
import EntryExpireForm from './EntryExpireForm';
import React from 'react';

type EntryActionsProps = {
    id: number;
    watch: Watch<EntryViewType>;
};

const EntryActions: React.FC<EntryActionsProps> = ({ id, watch }) => (
    <div className='actions'>
        <EntryChangeButton watch={watch} />
        <EntryExpireForm id={id} />
    </div>
);

export default React.memo(EntryActions);
