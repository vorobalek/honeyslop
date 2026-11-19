import { USER_AGENT_MAX_LENGTH } from '../../lib/constants';
import React from 'react';

type UserAgentSettingProps = {
    value: string | null;
    fallback: string;
    error: string | undefined;
};

const UserAgentSetting: React.FC<UserAgentSettingProps> = ({ value, fallback, error }) => (
    <div className='setting'>
        <label htmlFor='UserAgent'>User-Agent</label>
        <input
            type='text'
            id='UserAgent'
            name='UserAgent'
            maxLength={USER_AGENT_MAX_LENGTH}
            placeholder={fallback}
            defaultValue={value ?? ''}
        />
        {error && <span className='error'>{error}</span>}
        <p className='hint'>
            {`Sent with every WHOIS lookup at RIPEstat, so they know who asks and how to reach us. Empty uses "${fallback}" from the USER_AGENT variable.`}
        </p>
    </div>
);

export default React.memo(UserAgentSetting);
