import { useSelected } from '../../hooks/useSelection';
import { SelectionStore } from '../../lib/selectionStore';
import React from 'react';

type RowCheckboxProps = {
    id: number;
    selection: SelectionStore;
    disabled: boolean;
};

const RowCheckbox: React.FC<RowCheckboxProps> = ({ id, selection, disabled }) => {
    const checked = useSelected(selection, id);
    return (
        <input
            type='checkbox'
            value={id}
            aria-label={`Select entry ${id}`}
            disabled={disabled}
            checked={checked}
            onChange={(event) => selection.toggle(id, event.target.checked)}
        />
    );
};

export default React.memo(RowCheckbox);
