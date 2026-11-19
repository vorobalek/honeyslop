import { useAutoFilter } from '../../hooks/useAutoFilter';
import { ENTRY_STATES } from '../../lib/constants';
import { EntryStateType } from '../../types/entryStateType';
import Link from '../Common/Link';
import React from 'react';

type EntryFiltersProps = {
    host: string;
    target: string;
    state: EntryStateType | null;
};

const EntryFilters: React.FC<EntryFiltersProps> = ({ host, target, state }) => {
    const filter = useAutoFilter('/', { Host: host, Target: target, State: state ?? '' });
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
                Host{' '}
                <input
                    type='search'
                    name='Host'
                    defaultValue={host}
                    placeholder='contains'
                />
            </label>
            <label>
                Path{' '}
                <input
                    type='search'
                    name='Target'
                    defaultValue={target}
                    placeholder='contains'
                />
            </label>
            <label>
                State
                <select
                    name='State'
                    defaultValue={state ?? ''}
                >
                    <option value=''>any</option>
                    {ENTRY_STATES.map((value) => (
                        <option
                            key={value}
                            value={value}
                        >
                            {value.toLowerCase()}
                        </option>
                    ))}
                </select>
            </label>
            <Link
                className='button-link'
                to='/'
            >
                Reset
            </Link>
        </form>
    );
};

export default React.memo(EntryFilters);
