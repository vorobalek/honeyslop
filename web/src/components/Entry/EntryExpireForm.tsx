import { useSession } from '../../hooks/useSession';
import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { invokeHub } from '../../lib/hubConnection';
import { navigate } from '../../lib/navigation';
import { postNotice } from '../../lib/noticeStore';
import { expiredNotice } from '../Entries/expiredNotice';
import React, { type FormEvent } from 'react';

type EntryExpireFormProps = {
    id: number;
};

const EntryExpireForm: React.FC<EntryExpireFormProps> = ({ id }) => {
    const { canOperate } = useSession();
    const expire = async (event: FormEvent) => {
        event.preventDefault();
        const question = 'Expire this entry? It will be generated again on the next request.';
        if (!(await confirmAction({ question }))) return;
        const count = await invokeHub<number>('expireEntries', [id]);
        navigate('/');
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
                Expire
            </button>
        </form>
    );
};

export default React.memo(EntryExpireForm);
