import SignInLink from '../Layout/SignInLink';
import React from 'react';

const ReadOnlyNotice: React.FC = () => (
    <p className='notice'>
        {'Read-only view. '}
        <SignInLink />
        {' to change the settings.'}
    </p>
);

export default React.memo(ReadOnlyNotice);
