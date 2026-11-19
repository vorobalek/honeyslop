import { useWatched } from '../../hooks/useWatched';
import { directionOf, QueryValues } from '../../lib/listQuery';
import { Watch } from '../../lib/watchStore';
import { ClientsViewType } from '../../types/clientsViewType';
import { SortingType } from '../../types/sortingType';
import SortHeader from '../Common/SortHeader';
import ClientsTableRow from './ClientsTableRow';
import React from 'react';

type ClientsTableProps = {
    watch: Watch<ClientsViewType>;
    links: QueryValues;
    sorting: SortingType<string> | null;
};

const COLUMNS: ReadonlyArray<[string, string, string?]> = [
    ['Client', 'Client'],
    ['Requests', 'Requests', 'number'],
    ['Hosts', 'Hosts', 'number'],
    ['Paths', 'Paths', 'number'],
    ['FirstSeen', 'First seen'],
    ['LastSeen', 'Last seen'],
    ['UserAgent', 'Last User-Agent'],
];

const ClientsTable: React.FC<ClientsTableProps> = ({ watch, links, sorting }) => {
    const items = useWatched(watch, (view) => view.clients.items);
    const countries = useWatched(watch, (view) => view.countries);
    const delays = useWatched(watch, (view) => view.delays);
    const learnedDelays = useWatched(watch, (view) => view.learnedDelays);
    return (
        <div className='table-scroll'>
            <table className='entries'>
                <thead>
                    <tr>
                        {COLUMNS.map(([column, label, className]) => (
                            <SortHeader
                                key={column}
                                path='/clients'
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
                    {items.map((client) => (
                        <ClientsTableRow
                            key={client.clientIp}
                            client={client}
                            country={countries[client.clientIp]}
                            delay={delays[client.clientIp] ?? learnedDelays[client.clientIp]}
                            learned={
                                delays[client.clientIp] === undefined && learnedDelays[client.clientIp] !== undefined
                            }
                        />
                    ))}
                    {items.length === 0 && (
                        <tr>
                            <td
                                colSpan={7}
                                className='empty'
                            >
                                No clients in this period.
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
};

export default React.memo(ClientsTable);
