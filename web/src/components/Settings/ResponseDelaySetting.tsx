import { RESPONSE_DELAY_MAX_SECONDS } from '../../lib/constants';
import React from 'react';

type ResponseDelaySettingProps = {
    seconds: number | null;
    fallback: number;
    error: string | undefined;
};

const ResponseDelaySetting: React.FC<ResponseDelaySettingProps> = ({ seconds, fallback, error }) => (
    <div className='setting'>
        <label htmlFor='ResponseDelay'>Hold every response served from an entry for, seconds</label>
        <input
            type='number'
            id='ResponseDelay'
            name='ResponseDelay'
            min='0'
            max={RESPONSE_DELAY_MAX_SECONDS}
            step='1'
            placeholder={String(fallback)}
            defaultValue={seconds ?? ''}
        />
        {error && <span className='error'>{error}</span>}
        <p className='hint'>
            {`Keeps a scanner's connection open longer; an entry can set its own delay. Static files and plain 404s are never held. Empty uses ${fallback} s from the RESPONSE_DELAY_SECONDS variable.`}
        </p>
    </div>
);

export default React.memo(ResponseDelaySetting);
