import { FilterValues, useAutoFilter } from '../../hooks/useAutoFilter';
import { REQUEST_OUTCOMES } from '../../lib/constants';
import { RequestOutcomeType } from '../../types/requestOutcomeType';
import Link from '../Common/Link';
import React from 'react';

type RequestFiltersProps = {
    path: string;
    clientIp: string;
    clientIpLocked: boolean;
    host: string;
    target: string;
    outcome: RequestOutcomeType | null;
    entryId: number | null;
};

const RequestFilters: React.FC<RequestFiltersProps> = ({
    path,
    clientIp,
    clientIpLocked,
    host,
    target,
    outcome,
    entryId,
}) => {
    const applied: FilterValues = { Host: host, Target: target, Outcome: outcome ?? '' };
    if (!clientIpLocked) applied.ClientIp = clientIp;
    if (entryId !== null) applied.EntryId = String(entryId);
    const filter = useAutoFilter(path, applied);
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
                Client IP{' '}
                <input
                    type='search'
                    name='ClientIp'
                    defaultValue={clientIp}
                    placeholder='exact'
                    disabled={clientIpLocked}
                />
            </label>
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
                Outcome
                <select
                    name='Outcome'
                    defaultValue={outcome ?? ''}
                >
                    <option value=''>any</option>
                    {REQUEST_OUTCOMES.map((value) => (
                        <option
                            key={value}
                            value={value}
                        >
                            {value.toLowerCase()}
                        </option>
                    ))}
                </select>
            </label>
            {entryId !== null && (
                <input
                    type='hidden'
                    name='EntryId'
                    value={entryId}
                />
            )}
            <Link
                className='button-link'
                to={path}
            >
                Reset
            </Link>
        </form>
    );
};

export default React.memo(RequestFilters);
