import { useSession } from '../../hooks/useSession';
import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { BlockedContactsViewType } from '../../types/blockedContactsViewType';
import BlockedContactRow from './BlockedContactRow';
import React from 'react';

type BlockedContactsTableProps = {
    watch: Watch<BlockedContactsViewType>;
};

const BlockedContactsTable: React.FC<BlockedContactsTableProps> = ({ watch }) => {
    const { canOperate } = useSession();
    const items = useWatched(watch, (view) => view.contacts.items);
    return (
        <div className='table-scroll'>
            <table className='entries'>
                <thead>
                    <tr>
                        <th>Contact</th>
                        <th>Why</th>
                        {canOperate && <th>By</th>}
                        <th>Blocked</th>
                        {canOperate && <th />}
                    </tr>
                </thead>
                <tbody>
                    {items.length === 0 ? (
                        <tr>
                            <td
                                colSpan={canOperate ? 5 : 3}
                                className='hint'
                            >
                                Nothing is blocked.
                            </td>
                        </tr>
                    ) : (
                        items.map((contact) => (
                            <BlockedContactRow
                                key={contact.contact}
                                contact={contact}
                            />
                        ))
                    )}
                </tbody>
            </table>
        </div>
    );
};

export default React.memo(BlockedContactsTable);
