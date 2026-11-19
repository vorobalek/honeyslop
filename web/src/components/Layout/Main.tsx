import { useBusy } from '../../hooks/useBusy';
import Routes from '../Router/Routes';
import React from 'react';

const Main: React.FC = () => {
    const busy = useBusy();
    return (
        <main
            className='container'
            aria-busy={busy}
        >
            <Routes />
        </main>
    );
};

export default React.memo(Main);
