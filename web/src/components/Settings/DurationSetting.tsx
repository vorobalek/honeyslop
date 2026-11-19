import { DURATION_UNITS, splitDuration } from '../../lib/durations';
import React, { type ReactNode } from 'react';

type DurationSettingProps = {
    name: string;
    label: string;
    seconds: number;
    error: string | undefined;
    children: ReactNode;
};

const DurationSetting: React.FC<DurationSettingProps> = ({ name, label, seconds, error, children }) => {
    const { value, unit } = splitDuration(seconds);
    return (
        <div className='setting'>
            <label htmlFor={`${name}Value`}>{label}</label>
            <div className='duration'>
                <input
                    type='number'
                    min='0'
                    step='1'
                    id={`${name}Value`}
                    name={`${name}Value`}
                    defaultValue={value}
                />
                <select
                    id={`${name}Unit`}
                    name={`${name}Unit`}
                    defaultValue={unit}
                >
                    {DURATION_UNITS.map((option) => (
                        <option
                            key={option}
                            value={option}
                        >
                            {option}
                        </option>
                    ))}
                </select>
            </div>
            {error && <span className='error'>{error}</span>}
            <p className='hint'>{children}</p>
        </div>
    );
};

export default React.memo(DurationSetting);
