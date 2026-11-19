import { useSelectionSize } from '../../hooks/useSelection';
import { useSession } from '../../hooks/useSession';
import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { invokeHub } from '../../lib/hubConnection';
import { postNotice } from '../../lib/noticeStore';
import { SelectionStore } from '../../lib/selectionStore';
import { expiredNotice } from './expiredNotice';
import React, { type FormEvent } from 'react';

type ExpireSelectedButtonProps = {
    selection: SelectionStore;
};

const ExpireSelectedButton: React.FC<ExpireSelectedButtonProps> = ({ selection }) => {
    const { canOperate } = useSession();
    const selected = useSelectionSize(selection);
    const expire = async (event: FormEvent) => {
        event.preventDefault();
        const ids = selection.ids();
        const subject = ids.length === 1 ? 'the selected entry' : `${ids.length} selected entries`;
        const regenerated = ids.length === 1 ? 'It will' : 'They will';
        const question = `Expire ${subject}? ${regenerated} be generated again on the next request.`;
        if (!(await confirmAction({ question }))) return;
        const count = await invokeHub<number>('expireEntries', ids);
        selection.replace([]);
        postNotice(expiredNotice(count));
    };
    return (
        <form
            id='bulk'
            onSubmit={(event) => perform(() => expire(event))}
        >
            <button
                type='submit'
                className='danger'
                disabled={!canOperate || selected === 0}
                title={canOperate ? undefined : 'Sign in to expire entries'}
            >
                {selected > 0 ? `Expire ${selected} selected` : 'Expire selected'}
            </button>
        </form>
    );
};

export default React.memo(ExpireSelectedButton);
