import { useSession } from '../../hooks/useSession';
import { navigate } from '../../lib/navigation';
import { postNotice } from '../../lib/noticeStore';
import { ResponseChangeModeType } from '../../types/responseChangeModeType';
import ResponseChangeDialog from '../ResponseChange/ResponseChangeDialog';
import { createdNotice } from '../ResponseChange/changedNotice';
import React, { useState } from 'react';

const NO_IDS: readonly number[] = [];

function showCreated(mode: ResponseChangeModeType, ids: number[]): void {
    navigate(`/entries/${ids[0]}`);
    postNotice(createdNotice(mode));
}

const NewEntryButton: React.FC = () => {
    const { canOperate } = useSession();
    const [open, setOpen] = useState(false);
    return (
        <>
            <button
                type='button'
                disabled={!canOperate}
                title={canOperate ? undefined : 'Sign in to create entries'}
                onClick={() => setOpen(true)}
            >
                New entry
            </button>
            {open && (
                <ResponseChangeDialog
                    ids={NO_IDS}
                    current={null}
                    creating
                    onChanged={showCreated}
                    onClose={() => setOpen(false)}
                />
            )}
        </>
    );
};

export default React.memo(NewEntryButton);
