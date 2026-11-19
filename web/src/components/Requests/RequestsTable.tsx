import { useWatched } from '../../hooks/useWatched';
import { directionOf, QueryValues } from '../../lib/listQuery';
import { Watch } from '../../lib/watchStore';
import { RequestsViewType } from '../../types/requestsViewType';
import { SortingType } from '../../types/sortingType';
import SortHeader from '../Common/SortHeader';
import RequestsTableRow from './RequestsTableRow';
import React from 'react';

type RequestsTableProps = {
    watch: Watch<RequestsViewType>;
    path: string;
    links: QueryValues;
    sorting: SortingType<string> | null;
};

const COLUMNS: ReadonlyArray<[string, string, string?]> = [
    ['Time', 'Time'],
    ['Client', 'Client'],
    ['Method', 'Method'],
    ['Host', 'Host'],
    ['Path', 'Path'],
    ['Outcome', 'Outcome'],
    ['Code', 'Code', 'number'],
    ['Duration', 'ms', 'number'],
    ['UserAgent', 'User-Agent'],
];

const RequestsTable: React.FC<RequestsTableProps> = ({ watch, path, links, sorting }) => {
    const items = useWatched(watch, (view) => view.requests.items);
    const countries = useWatched(watch, (view) => view.countries);
    return (
        <div className='table-scroll'>
            <table className='entries'>
                <thead>
                    <tr>
                        {COLUMNS.map(([column, label, className]) => (
                            <SortHeader
                                key={column}
                                path={path}
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
                    {items.map((request) => (
                        <RequestsTableRow
                            key={request.id}
                            request={request}
                            country={countries[request.clientIp]}
                        />
                    ))}
                    {items.length === 0 && (
                        <tr>
                            <td
                                colSpan={9}
                                className='empty'
                            >
                                No requests recorded yet.
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
};

export default React.memo(RequestsTable);
