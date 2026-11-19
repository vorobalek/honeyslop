import { useNotice } from '../../hooks/useNotice';
import React from 'react';

const PageNotice: React.FC = () => {
    const notice = useNotice();
    return notice === null ? null : <p className='notice'>{notice}</p>;
};

export default React.memo(PageNotice);
