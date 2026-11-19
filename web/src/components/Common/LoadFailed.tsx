import React from 'react';

const LoadFailed: React.FC = () => (
    <p className='error'>This page failed to load. It is tried again on the next change.</p>
);

export default React.memo(LoadFailed);
