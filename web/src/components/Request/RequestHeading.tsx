import { outcomeBadge } from '../../lib/badges';
import { RequestRecordType } from '../../types/requestRecordType';
import React from 'react';

type RequestHeadingProps = {
    record: RequestRecordType;
};

const RequestHeading: React.FC<RequestHeadingProps> = ({ record }) => (
    <h1 className='request-line'>
        <span className={outcomeBadge(record.outcome)}>{record.outcome.toLowerCase()}</span>
        <span className='mono'>{record.method}</span>
        <span className='mono host'>{record.host}</span>
        <span className='mono target'>{record.target}</span>
    </h1>
);

export default React.memo(RequestHeading);
