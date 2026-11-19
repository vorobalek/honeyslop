import { useSession } from '../../hooks/useSession';
import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { invokeHub } from '../../lib/hubConnection';
import { navigate } from '../../lib/navigation';
import { postNotice } from '../../lib/noticeStore';
import { expiredNotice } from './expiredNotice';
import React, { type FormEvent } from 'react';

const ExpireAllButton: React.FC = () => {
    const { canOperate } = useSession();
    const expire = async (event: FormEvent) => {
        event.preventDefault();
        const question = 'Expire every cached entry? All responses will be generated again.';
        if (!(await confirmAction({ question }))) return;
        const count = await invokeHub<number>('expireMatching', {});
        navigate('/', { replace: true });
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
                Expire everything
            </button>
        </form>
    );
};

export default React.memo(ExpireAllButton);
