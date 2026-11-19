import { useQuery } from '../../hooks/useLocation';
import { useTitle } from '../../hooks/useTitle';
import { useWatch } from '../../hooks/useWatch';
import { useWatchPresence } from '../../hooks/useWatched';
import { ENTRY_COLUMNS, ENTRY_STATES } from '../../lib/constants';
import { oneOf, pageParam, sortingParam, textParam } from '../../lib/listQuery';
import { createSelectionStore } from '../../lib/selectionStore';
import { EntriesViewType } from '../../types/entriesViewType';
import ListPager from '../Common/ListPager';
import PageNotice from '../Common/PageNotice';
import Presence from '../Common/Presence';
import EntryFilters from '../Filters/EntryFilters';
import EntriesActions from './EntriesActions';
import EntriesStats from './EntriesStats';
import EntriesTable from './EntriesTable';
import React, { useEffect, useMemo } from 'react';

const selectEntries = (view: EntriesViewType) => view.entries;

const EntriesPage: React.FC = () => {
    useTitle('Entries');
    const query = useQuery();
    const { host, target, state, sorting, page } = useMemo(
        () => ({
            host: textParam(query, 'Host'),
            target: textParam(query, 'Target'),
            state: oneOf(query, 'State', ENTRY_STATES),
            sorting: sortingParam(query, ENTRY_COLUMNS),
            page: pageParam(query),
        }),
        [query],
    );
    const filter = useMemo(() => ({ host: host || null, target: target || null, state }), [host, target, state]);
    const links = useMemo(() => ({ Host: host, Target: target, State: state }), [host, target, state]);
    const watch = useWatch({ view: 'entries', query: { filter, sort: sorting, page } });
    const presence = useWatchPresence(watch);
    const selection = useMemo(() => createSelectionStore(), []);
    useEffect(() => selection.replace([]), [watch, selection]);

    if (presence !== 'present') return <Presence presence={presence} />;
    return (
        <>
            <PageNotice />
            <EntryFilters
                host={host}
                target={target}
                state={state}
            />
            <div>
                <EntriesStats watch={watch} />
                <EntriesActions
                    watch={watch}
                    selection={selection}
                    filter={filter}
                />
                <EntriesTable
                    watch={watch}
                    selection={selection}
                    links={links}
                    sorting={sorting}
                />
                <ListPager
                    watch={watch}
                    select={selectEntries}
                    path='/'
                    filter={links}
                    sorting={sorting}
                    newer='← Newer'
                />
            </div>
        </>
    );
};

export default React.memo(EntriesPage);
