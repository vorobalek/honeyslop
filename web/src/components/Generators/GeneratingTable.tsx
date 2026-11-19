import { SortableColumn, useClientSort } from '../../hooks/useClientSort';
import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { EntryRowType } from '../../types/entryRowType';
import { GeneratorsViewType } from '../../types/generatorsViewType';
import ClientSortHeader from '../Common/ClientSortHeader';
import GeneratingTableRow from './GeneratingTableRow';
import { runningFor, waitedBefore } from './entryTimes';
import React from 'react';

type GeneratingTableProps = {
    watch: Watch<GeneratorsViewType>;
};

const COLUMNS: readonly SortableColumn<EntryRowType, string>[] = [
    { label: 'Entry', value: (entry) => String(entry.id) },
    { label: 'Method', value: (entry) => entry.method },
    { label: 'Host', value: (entry) => entry.host },
    { label: 'Path', value: (entry) => entry.target },
    { label: 'Generator', value: (entry) => entry.generator ?? 'waiting for a slot' },
    { label: 'Attempt', className: 'number', value: (entry) => String(entry.attempts + 1) },
    { label: 'Running for', className: 'number', value: (entry, asOf) => String(runningFor(entry, asOf)) },
    { label: 'Waited before', className: 'number', value: (entry, asOf) => String(waitedBefore(entry, asOf)) },
];

const GeneratingTable: React.FC<GeneratingTableProps> = ({ watch }) => {
    const rows = useWatched(watch, (view) => view.generating);
    const asOf = useWatched(watch, (view) => view.asOf);
    const [sorted, state, toggle] = useClientSort(rows, COLUMNS, asOf);
    return (
        <div className='table-scroll'>
            <table
                className='entries'
                data-sortable='generating'
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
                        <GeneratingTableRow
                            key={entry.id}
                            entry={entry}
                            asOf={asOf}
                        />
                    ))}
                    {rows.length === 0 && (
                        <tr>
                            <td
                                colSpan={8}
                                className='empty'
                            >
                                Nothing is being generated.
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
};

export default React.memo(GeneratingTable);
