import { useQuery } from '../../hooks/useLocation';
import { useTitle } from '../../hooks/useTitle';
import { useWatch } from '../../hooks/useWatch';
import { useWatchPresence } from '../../hooks/useWatched';
import { pageParam, textParam } from '../../lib/listQuery';
import { BlockedContactsViewType } from '../../types/blockedContactsViewType';
import Link from '../Common/Link';
import ListPager from '../Common/ListPager';
import PageNotice from '../Common/PageNotice';
import Presence from '../Common/Presence';
import BlockContactsForm from './BlockContactsForm';
import BlockedContactsCount from './BlockedContactsCount';
import BlockedContactsFilter from './BlockedContactsFilter';
import BlockedContactsTable from './BlockedContactsTable';
import React, { useMemo } from 'react';

const selectContacts = (view: BlockedContactsViewType) => view.contacts;

const BlockedContactsPage: React.FC = () => {
    useTitle('Blocked contacts');
    const query = useQuery();
    const search = textParam(query, 'Search');
    const page = pageParam(query);
    const links = useMemo(() => ({ Search: search }), [search]);
    const watch = useWatch({ view: 'blockedContacts', query: { search: search || null, page } });
    const presence = useWatchPresence(watch);
    if (presence !== 'present') return <Presence presence={presence} />;
    return (
        <>
            <p className='back'>
                <Link to='/reports'>← All reports</Link>
            </p>
            <PageNotice />
            <BlockContactsForm />
            <BlockedContactsFilter search={search} />
            <div>
                <div className='actions'>
                    <BlockedContactsCount watch={watch} />
                </div>
                <BlockedContactsTable watch={watch} />
                <ListPager
                    watch={watch}
                    select={selectContacts}
                    path='/reports/blocked'
                    filter={links}
                    sorting={null}
                    newer='← Newer'
                />
            </div>
        </>
    );
};

export default React.memo(BlockedContactsPage);
