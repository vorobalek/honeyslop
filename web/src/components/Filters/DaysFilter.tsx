import { currentSorting, href } from '../../lib/listQuery';
import { navigate } from '../../lib/navigation';
import React, { type ChangeEvent } from 'react';

const PERIODS: ReadonlyArray<[string, string]> = [
    ['1', '24 hours'],
    ['7', '7 days'],
    ['30', '30 days'],
    ['0', 'all history'],
];

type DaysFilterProps = {
    days: number;
};

const DaysFilter: React.FC<DaysFilterProps> = ({ days }) => {
    const onChange = (event: ChangeEvent<HTMLSelectElement>) =>
        navigate(href('/clients', { ...currentSorting(), Days: event.target.value }), {
            replace: true,
        });
    return (
        <form
            className='filters'
            method='get'
            onSubmit={(event) => event.preventDefault()}
        >
            <label>
                Seen within
                <select
                    name='Days'
                    value={days <= 0 ? '0' : String(days)}
                    onChange={onChange}
                >
                    {PERIODS.map(([value, label]) => (
                        <option
                            key={value}
                            value={value}
                        >
                            {label}
                        </option>
                    ))}
                </select>
            </label>
        </form>
    );
};

export default React.memo(DaysFilter);
