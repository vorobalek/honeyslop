import Link from '../Common/Link';
import React from 'react';

const SECTIONS: ReadonlyArray<[string, string]> = [
    ['/', 'Entries'],
    ['/requests', 'Requests'],
    ['/clients', 'Clients'],
    ['/reports', 'Reports'],
    ['/generators', 'Generators'],
    ['/settings', 'Settings'],
];

const Navigation: React.FC = () => (
    <nav className='nav'>
        {SECTIONS.map(([to, label]) => (
            <Link
                key={to}
                to={to}
            >
                {label}
            </Link>
        ))}
    </nav>
);

export default React.memo(Navigation);
