import { display } from '../../lib/time';
import { ReportBounceType } from '../../types/reportBounceType';
import React from 'react';

type ReportBouncesProps = {
    bounces: readonly ReportBounceType[];
};

const ReportBounces: React.FC<ReportBouncesProps> = ({ bounces }) => (
    <>
        {bounces.map((bounce, index) => (
            <div key={index}>
                <span className='mono'>{bounce.recipient}</span>{' '}
                <span className='error'>
                    {[bounce.action === 'Failed' ? 'failed' : 'delayed', bounce.status].filter(Boolean).join(' ')}
                </span>{' '}
                <span className='hint'>
                    {[bounce.mta, `${display(bounce.bouncedAt)} UTC`].filter(Boolean).join(' · ')}
                </span>
                {bounce.diagnostic && <div className='hint'>{bounce.diagnostic}</div>}
            </div>
        ))}
    </>
);

export default React.memo(ReportBounces);
