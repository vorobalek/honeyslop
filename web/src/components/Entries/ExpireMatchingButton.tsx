import { useSession } from '../../hooks/useSession';
import { useWatched } from '../../hooks/useWatched';
import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { invokeHub } from '../../lib/hubConnection';
import { firstPageHref } from '../../lib/listQuery';
import { navigate } from '../../lib/navigation';
import { postNotice } from '../../lib/noticeStore';
import { Watch } from '../../lib/watchStore';
import { EntriesViewType } from '../../types/entriesViewType';
import { EntriesFilter } from './EntriesActions';
import { expiredNotice } from './expiredNotice';
import React, { type FormEvent } from 'react';

type ExpireMatchingButtonProps = {
    watch: Watch<EntriesViewType>;
    filter: EntriesFilter;
};

const ExpireMatchingButton: React.FC<ExpireMatchingButtonProps> = ({ watch, filter }) => {
    const { canOperate } = useSession();
    const total = useWatched(watch, (view) => view.entries.total);
    if (total === 0) return null;
    const expire = async (event: FormEvent) => {
        event.preventDefault();
        if (!(await confirmAction({ question: `Expire all ${total} entries matching the filter?` }))) return;
        const count = await invokeHub<number>('expireMatching', filter);
        navigate(firstPageHref(), { replace: true });
        postNotice(expiredNotice(count));
    };
    return (
        <form onSubmit={(event) => perform(() => expire(event))}>
            <button
                type='submit'
                className='danger'
                disabled={!canOperate}
                title={canOperate ? undefined : 'Sign in to expire entries'}
            >
                {`Expire ${total} matching`}
            </button>
        </form>
    );
};

export default React.memo(ExpireMatchingButton);
