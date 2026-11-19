import { useSession } from '../../hooks/useSession';
import { perform } from '../../lib/actions';
import { unblockContact } from '../../lib/blockedContacts';
import { display } from '../../lib/time';
import { BlockedContactType } from '../../types/blockedContactType';
import React from 'react';

type BlockedContactRowProps = {
    contact: BlockedContactType;
};

const BlockedContactRow: React.FC<BlockedContactRowProps> = ({ contact }) => {
    const { canOperate } = useSession();
    return (
        <tr>
            <td className='mono'>{contact.contact}</td>
            <td>{contact.note ?? '—'}</td>
            {canOperate && <td>{contact.createdBy ?? '—'}</td>}
            <td>{display(contact.createdAt)}</td>
            {canOperate && (
                <td>
                    <button
                        type='button'
                        className='link danger'
                        onClick={() => perform(() => unblockContact(contact.contact))}
                    >
                        Unblock
                    </button>
                </td>
            )}
        </tr>
    );
};

export default React.memo(BlockedContactRow);
