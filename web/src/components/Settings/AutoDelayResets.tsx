import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { invokeHub } from '../../lib/hubConnection';
import { postNotice } from '../../lib/noticeStore';
import { countOf } from '../../lib/numbers';
import { ClientDelaysChangeType } from '../../types/clientDelaysChangeType';
import React from 'react';

async function resetLearned(): Promise<void> {
    const confirmed = await confirmAction({
        question: 'Forget every learned delay?',
        detail: 'Every client starts again from the middle of the range.',
        button: 'Forget',
    });
    if (!confirmed) return;
    const result = await invokeHub<ClientDelaysChangeType>('resetLearnedDelays');
    postNotice(`Forgot ${countOf(result.changed, 'learned delay')}.`);
}

async function resetManual(): Promise<void> {
    const confirmed = await confirmAction({
        question: 'Remove every delay set by hand?',
        detail: 'Learned delays and the defaults apply to those clients again.',
        button: 'Remove',
    });
    if (!confirmed) return;
    const result = await invokeHub<ClientDelaysChangeType>('resetClientDelays');
    postNotice(`Removed ${countOf(result.changed, 'delay')} set by hand.`);
}

const AutoDelayResets: React.FC = () => (
    <div className='setting'>
        <div className='actions'>
            <button
                type='button'
                className='danger'
                onClick={() => perform(resetLearned)}
            >
                Reset learning
            </button>
            <button
                type='button'
                className='danger'
                onClick={() => perform(resetManual)}
            >
                Reset manual delays
            </button>
        </div>
        <p className='hint'>These take effect at once, without Save.</p>
    </div>
);

export default React.memo(AutoDelayResets);
