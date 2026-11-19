import Link from '../Common/Link';
import Account from './Account';
import LiveIndicator from './LiveIndicator';
import Navigation from './Navigation';
import React from 'react';

const Header: React.FC = () => (
    <header className='topbar'>
        <Link
            className='brand'
            to='/'
        >
            honeyslop
        </Link>
        <span className='subtitle'>LLM honeypot</span>
        <LiveIndicator />
        <Navigation />
        <Account />
    </header>
);

export default React.memo(Header);
