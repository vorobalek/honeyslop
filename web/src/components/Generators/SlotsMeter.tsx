import { LlmServerStatusType } from '../../types/llmServerStatusType';
import React from 'react';

type SlotsMeterProps = {
    status: LlmServerStatusType;
};

const SlotsMeter: React.FC<SlotsMeterProps> = ({ status }) => (
    <>
        <span className='slots'>
            {Array.from({ length: status.slots }, (_, slot) => (
                <span
                    key={slot}
                    className={`slot ${slot < status.busySlots ? 'slot-busy' : 'slot-idle'}`}
                />
            ))}
        </span>
        {` ${status.busySlots}/${status.slots}`}
    </>
);

export default React.memo(SlotsMeter);
