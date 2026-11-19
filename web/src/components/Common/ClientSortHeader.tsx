import { ClientSortState, SortableColumn } from '../../hooks/useClientSort';
import React from 'react';

type ClientSortHeaderProps<T, C> = {
    columns: readonly SortableColumn<T, C>[];
    state: ClientSortState;
    onToggle: (index: number) => void;
};

function ClientSortHeader<T, C>({ columns, state, onToggle }: ClientSortHeaderProps<T, C>) {
    return (
        <tr>
            {columns.map((column, index) => {
                const current = state?.index === index ? state.direction : null;
                const sorted = current === 'Asc' ? 'sorted-asc' : current === 'Desc' ? 'sorted-desc' : '';
                return (
                    <th
                        key={column.label}
                        className={[column.className, 'sortable', sorted].filter(Boolean).join(' ')}
                        aria-sort={current === 'Asc' ? 'ascending' : current === 'Desc' ? 'descending' : 'none'}
                        onClick={() => onToggle(index)}
                    >
                        {column.label}
                    </th>
                );
            })}
        </tr>
    );
}

export default React.memo(ClientSortHeader) as typeof ClientSortHeader;
