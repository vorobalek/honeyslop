import { useSelectionSizeExcept } from '../../hooks/useSelection';
import { useSession } from '../../hooks/useSession';
import { useWatched } from '../../hooks/useWatched';
import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { invokeHub } from '../../lib/hubConnection';
import { postNotice } from '../../lib/noticeStore';
import { SelectionStore } from '../../lib/selectionStore';
import { Watch } from '../../lib/watchStore';
import { EntriesViewType } from '../../types/entriesViewType';
import { expiredNotice } from './expiredNotice';
import React, { useMemo, type FormEvent } from 'react';

type ExpireSelectedButtonProps = {
    watch: Watch<EntriesViewType>;
    selection: SelectionStore;
};

const selectExpiredIds = (view: EntriesViewType) =>
    view.entries.items
        .filter((entry) => entry.state === 'Expired')
        .map((entry) => entry.id)
        .join(',');

const ExpireSelectedButton: React.FC<ExpireSelectedButtonProps> = ({ watch, selection }) => {
    const { canOperate } = useSession();
    const expiredIds = useWatched(watch, selectExpiredIds);
    const expired = useMemo(() => new Set(expiredIds === '' ? [] : expiredIds.split(',').map(Number)), [expiredIds]);
    const selected = useSelectionSizeExcept(selection, expired);
    const expire = async (event: FormEvent) => {
        event.preventDefault();
        const ids = selection.ids().filter((id) => !expired.has(id));
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
