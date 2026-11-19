import React from 'react';

type WhoisUnavailableProps = {
    onRetry: () => void;
};

const WhoisUnavailable: React.FC<WhoisUnavailableProps> = ({ onRetry }) => (
    <p className='error'>
        {'RIPEstat did not answer. '}
        <button
            type='button'
            onClick={onRetry}
        >
            Try again
        </button>
    </p>
);

export default React.memo(WhoisUnavailable);
