import { useTitle } from '../../hooks/useTitle';
import React from 'react';

const NotFound: React.FC = () => {
    useTitle('Not found');
    return <p className='notice'>There is nothing here.</p>;
};

export default React.memo(NotFound);
