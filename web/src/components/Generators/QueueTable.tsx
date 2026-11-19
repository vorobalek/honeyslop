import { SortableColumn, useClientSort } from '../../hooks/useClientSort';
import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { EntryRowType } from '../../types/entryRowType';
import { GeneratorsViewType } from '../../types/generatorsViewType';
import ClientSortHeader from '../Common/ClientSortHeader';
import QueueTableRow from './QueueTableRow';
import { queuedFor, requestedAgo } from './entryTimes';
import React from 'react';

type QueueTableProps = {
    watch: Watch<GeneratorsViewType>;
};

const COLUMNS: readonly SortableColumn<EntryRowType, string>[] = [
    { label: 'Entry', value: (entry) => String(entry.id) },
    { label: 'Method', value: (entry) => entry.method },
    { label: 'Host', value: (entry) => entry.host },
    { label: 'Path', value: (entry) => entry.target },
    { label: 'Queued for', className: 'number', value: (entry, asOf) => String(queuedFor(entry, asOf)) },
    { label: 'Last requested', className: 'number', value: (entry, asOf) => String(requestedAgo(entry, asOf) ?? '') },
];

const QueueTable: React.FC<QueueTableProps> = ({ watch }) => {
    const rows = useWatched(watch, (view) => view.queued);
    const asOf = useWatched(watch, (view) => view.asOf);
    const [sorted, state, toggle] = useClientSort(rows, COLUMNS, asOf);
    return (
        <div className='table-scroll'>
            <table
                className='entries'
                data-sortable='queued'
            >
                <thead>
                    <ClientSortHeader
                        columns={COLUMNS}
                        state={state}
                        onToggle={toggle}
                    />
                </thead>
                <tbody>
                    {sorted.map((entry) => (
                        <QueueTableRow
                            key={entry.id}
                            entry={entry}
                            asOf={asOf}
                        />
                    ))}
                    {rows.length === 0 && (
                        <tr>
                            <td
                                colSpan={6}
                                className='empty'
                            >
                                The queue is empty.
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
};

export default React.memo(QueueTable);
