import { SelectionStore } from '../../lib/selectionStore';
import { Watch } from '../../lib/watchStore';
import { EntriesViewType } from '../../types/entriesViewType';
import { EntryStateType } from '../../types/entryStateType';
import EntriesCount from './EntriesCount';
import ExpireAllButton from './ExpireAllButton';
import ExpireMatchingButton from './ExpireMatchingButton';
import ExpireSelectedButton from './ExpireSelectedButton';
import React from 'react';

export type EntriesFilter = {
    host: string | null;
    target: string | null;
    state: EntryStateType | null;
};

type EntriesActionsProps = {
    watch: Watch<EntriesViewType>;
    selection: SelectionStore;
    filter: EntriesFilter;
};

const EntriesActions: React.FC<EntriesActionsProps> = ({ watch, selection, filter }) => {
    const filtered = filter.host !== null || filter.target !== null || filter.state !== null;
    return (
        <div className='actions'>
            <ExpireSelectedButton selection={selection} />
            {filtered && (
                <ExpireMatchingButton
                    watch={watch}
                    filter={filter}
                />
            )}
            <ExpireAllButton />
            <EntriesCount watch={watch} />
        </div>
    );
};

export default React.memo(EntriesActions);
