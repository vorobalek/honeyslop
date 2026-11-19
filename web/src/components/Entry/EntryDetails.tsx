import { useTitle } from '../../hooks/useTitle';
import { useWatched } from '../../hooks/useWatched';
import { href } from '../../lib/listQuery';
import { Watch } from '../../lib/watchStore';
import { EntryViewType } from '../../types/entryViewType';
import Link from '../Common/Link';
import PageNotice from '../Common/PageNotice';
import EntryExpireForm from './EntryExpireForm';
import EntryHeading from './EntryHeading';
import EntryMeta from './EntryMeta';
import EntryOutput from './EntryOutput';
import EntryRequest from './EntryRequest';
import EntryResponse from './EntryResponse';
import EntryStateNotice from './EntryStateNotice';
import React from 'react';

type EntryDetailsProps = {
    id: number;
    watch: Watch<EntryViewType>;
};

const EntryDetails: React.FC<EntryDetailsProps> = ({ id, watch }) => {
    const state = useWatched(watch, (view) => view.state);
    const title = useWatched(watch, (view) => `${view.entry.method} ${view.entry.host}${view.entry.target}`);
    useTitle(title);
    return (
        <>
            <p className='back'>
                <Link to='/'>← All entries</Link>
            </p>
            <PageNotice />
            <EntryStateNotice watch={watch} />
            <EntryHeading watch={watch} />
            {state !== 'Expired' && <EntryExpireForm id={id} />}
            <p>
                <Link to={href('/requests', { EntryId: id })}>Every request answered with this entry →</Link>
            </p>
            <EntryMeta watch={watch} />
            {state === 'Generating' && <EntryOutput watch={watch} />}
            <EntryRequest watch={watch} />
            <EntryResponse watch={watch} />
        </>
    );
};

export default React.memo(EntryDetails);
