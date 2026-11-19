import { useSelectionSize } from '../../hooks/useSelection';
import { useWatched } from '../../hooks/useWatched';
import { SelectionStore } from '../../lib/selectionStore';
import { Watch } from '../../lib/watchStore';
import { EntriesViewType } from '../../types/entriesViewType';
import React, { useMemo } from 'react';

type SelectAllCheckboxProps = {
    watch: Watch<EntriesViewType>;
    selection: SelectionStore;
    disabled: boolean;
};

const SelectAllCheckbox: React.FC<SelectAllCheckboxProps> = ({ watch, selection, disabled }) => {
    const idsKey = useWatched(watch, (view) => view.entries.items.map((entry) => entry.id).join(','));
    const ids = useMemo(() => (idsKey === '' ? [] : idsKey.split(',').map(Number)), [idsKey]);
    const selected = useSelectionSize(selection);
    const all = ids.length > 0 && selected === ids.length && ids.every((id) => selection.has(id));
    return (
        <input
            type='checkbox'
            aria-label='Select all'
            disabled={disabled}
            checked={all}
            onChange={(event) => selection.replace(event.target.checked ? ids : [])}
        />
    );
};

export default React.memo(SelectAllCheckbox);
