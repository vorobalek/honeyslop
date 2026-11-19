import { useClientSort } from '../../hooks/useClientSort';
import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { GeneratorsViewType } from '../../types/generatorsViewType';
import ClientSortHeader from '../Common/ClientSortHeader';
import GeneratorsTableRow from './GeneratorsTableRow';
import { GENERATOR_COLUMNS } from './generatorColumns';
import React from 'react';

type GeneratorsTableProps = {
    watch: Watch<GeneratorsViewType>;
};

const GeneratorsTable: React.FC<GeneratorsTableProps> = ({ watch }) => {
    const generators = useWatched(watch, (view) => view.generators);
    const [sorted, state, toggle] = useClientSort(generators, GENERATOR_COLUMNS, undefined);
    return (
        <div className='table-scroll'>
            <table
                className='entries'
                data-sortable='generators'
            >
                <thead>
                    <ClientSortHeader
                        columns={GENERATOR_COLUMNS}
                        state={state}
                        onToggle={toggle}
                    />
                </thead>
                <tbody>
                    {sorted.map((generator) => (
                        <GeneratorsTableRow
                            key={generator.name}
                            generator={generator}
                        />
                    ))}
                    {generators.length === 0 && (
                        <tr>
                            <td
                                colSpan={12}
                                className='empty'
                            >
                                No model servers found.
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
};

export default React.memo(GeneratorsTable);
