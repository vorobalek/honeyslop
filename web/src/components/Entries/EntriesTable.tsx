import { useSession } from '../../hooks/useSession';
import { useWatched } from '../../hooks/useWatched';
import { directionOf, QueryValues } from '../../lib/listQuery';
import { SelectionStore } from '../../lib/selectionStore';
import { Watch } from '../../lib/watchStore';
import { EntriesViewType } from '../../types/entriesViewType';
import { SortingType } from '../../types/sortingType';
import SortHeader from '../Common/SortHeader';
import EntriesTableRow from './EntriesTableRow';
import SelectAllCheckbox from './SelectAllCheckbox';
import React from 'react';

type EntriesTableProps = {
    watch: Watch<EntriesViewType>;
    selection: SelectionStore;
    links: QueryValues;
    sorting: SortingType<string> | null;
};

const COLUMNS: ReadonlyArray<[string, string, string?]> = [
    ['Status', 'Status'],
    ['Method', 'Method'],
    ['Host', 'Host'],
    ['Path', 'Path'],
    ['Code', 'Code', 'number'],
    ['Size', 'Size', 'number'],
    ['Hits', 'Hits', 'number'],
    ['Created', 'Created'],
    ['LastHit', 'Last hit'],
];

const EntriesTable: React.FC<EntriesTableProps> = ({ watch, selection, links, sorting }) => {
    const { canOperate } = useSession();
    const items = useWatched(watch, (view) => view.entries.items);
    return (
        <div className='table-scroll'>
            <table className='entries'>
                <thead>
                    <tr>
                        <th className='check'>
                            <SelectAllCheckbox
                                watch={watch}
                                selection={selection}
                                disabled={!canOperate}
                            />
                        </th>
                        {COLUMNS.map(([column, label, className]) => (
                            <SortHeader
                                key={column}
                                path='/'
                                filter={links}
                                column={column}
                                current={directionOf(sorting, column)}
                                label={label}
                                className={className}
                            />
                        ))}
                    </tr>
                </thead>
                <tbody>
                    {items.map((entry) => (
                        <EntriesTableRow
                            key={entry.id}
                            entry={entry}
                            selection={selection}
                            disabled={!canOperate}
                        />
                    ))}
                    {items.length === 0 && (
                        <tr>
                            <td
                                colSpan={10}
                                className='empty'
                            >
                                Nothing here yet.
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
};

export default React.memo(EntriesTable);
