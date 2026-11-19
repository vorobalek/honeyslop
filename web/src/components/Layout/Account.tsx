import { useSession } from '../../hooks/useSession';
import SignInLink from './SignInLink';
import React from 'react';

const Account: React.FC = () => {
    const session = useSession();
    if (!session.signInEnabled) return null;
    if (session.signedIn) {
        return (
            <form
                className='account'
                method='post'
                action='/signout'
            >
                <span className='account-name'>{session.name}</span>
                <button
                    type='submit'
                    className='link'
                >
                    Sign out
                </button>
            </form>
        );
    }
    return (
        <span className='account'>
            <span className='account-name'>read-only</span>
            <SignInLink />
        </span>
    );
};

export default React.memo(Account);
