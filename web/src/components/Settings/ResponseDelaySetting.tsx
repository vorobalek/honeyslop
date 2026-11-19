import { RESPONSE_DELAY_MAX_MILLISECONDS } from '../../lib/constants';
import React from 'react';

type ResponseDelaySettingProps = {
    milliseconds: number | null;
    fallback: number;
    error: string | undefined;
};

const ResponseDelaySetting: React.FC<ResponseDelaySettingProps> = ({ milliseconds, fallback, error }) => (
    <div className='setting'>
        <label htmlFor='ResponseDelay'>Hold every response served from an entry for, milliseconds</label>
        <input
            type='number'
            id='ResponseDelay'
            name='ResponseDelay'
            min='0'
            max={RESPONSE_DELAY_MAX_MILLISECONDS}
            step='1'
            placeholder={String(fallback)}
            defaultValue={milliseconds ?? ''}
        />
        {error && <span className='error'>{error}</span>}
        <p className='hint'>
            {`Keeps a scanner's connection open longer; an entry can set its own delay and a client's delay wins over both. Static files and plain 404s are never held. Empty uses ${fallback} ms from the RESPONSE_DELAY_MILLISECONDS variable.`}
        </p>
    </div>
);

export default React.memo(ResponseDelaySetting);
