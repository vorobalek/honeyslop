import { useAutoFilter } from '../../hooks/useAutoFilter';
import Link from '../Common/Link';
import React from 'react';

type BlockedContactsFilterProps = {
    search: string;
};

const BlockedContactsFilter: React.FC<BlockedContactsFilterProps> = ({ search }) => {
    const filter = useAutoFilter('/reports/blocked', { Search: search });
    return (
        <form
            key={filter.formKey}
            className='filters'
            method='get'
            onInput={filter.onInput}
            onChange={filter.onChange}
            onSubmit={filter.onSubmit}
        >
            <label>
                Address, domain or reason{' '}
                <input
                    type='search'
                    name='Search'
                    defaultValue={search}
                    placeholder='contains'
                />
            </label>
            <Link
                className='button-link'
                to='/reports/blocked'
            >
                Reset
            </Link>
        </form>
    );
};

export default React.memo(BlockedContactsFilter);
