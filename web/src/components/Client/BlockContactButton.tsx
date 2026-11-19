import { useSession } from '../../hooks/useSession';
import { perform } from '../../lib/actions';
import { blockContacts } from '../../lib/blockedContacts';
import React from 'react';

type BlockContactButtonProps = {
    contact: string;
};

const BlockContactButton: React.FC<BlockContactButtonProps> = ({ contact }) => {
    const { canOperate } = useSession();
    if (!canOperate) return null;
    return (
        <button
            type='button'
            className='link danger'
            onClick={() =>
                perform(async () => {
                    await blockContacts([contact], null);
                })
            }
        >
            block
        </button>
    );
};

export default React.memo(BlockContactButton);
