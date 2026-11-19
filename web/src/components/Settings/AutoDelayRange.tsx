import { RESPONSE_DELAY_MAX_MILLISECONDS } from '../../lib/constants';
import React from 'react';

type AutoDelayRangeProps = {
    min: number;
    max: number;
    minError: string | undefined;
    maxError: string | undefined;
};

const AutoDelayRange: React.FC<AutoDelayRangeProps> = ({ min, max, minError, maxError }) => (
    <div className='setting'>
        <span className='label'>Hold Auto-Delay clients between, milliseconds</span>
        <div className='duration'>
            <input
                type='number'
                name='AutoDelayMin'
                aria-label='Shortest delay, milliseconds'
                min='0'
                max={RESPONSE_DELAY_MAX_MILLISECONDS}
                step='1'
                defaultValue={min}
            />
            <input
                type='number'
                name='AutoDelayMax'
                aria-label='Longest delay, milliseconds'
                min='1'
                max={RESPONSE_DELAY_MAX_MILLISECONDS}
                step='1'
                defaultValue={max}
            />
        </div>
        {minError && <span className='error'>{minError}</span>}
        {maxError && <span className='error'>{maxError}</span>}
        <p className='hint'>A new client starts in the middle; the learned delays never leave this range.</p>
    </div>
);

export default React.memo(AutoDelayRange);
