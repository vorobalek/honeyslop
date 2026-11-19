import { useSession } from '../../hooks/useSession';
import { navigate } from '../../lib/navigation';
import { postNotice } from '../../lib/noticeStore';
import { Watch } from '../../lib/watchStore';
import { EntryType } from '../../types/entryType';
import { EntryViewType } from '../../types/entryViewType';
import { ResponseChangeModeType } from '../../types/responseChangeModeType';
import ResponseChangeDialog from '../ResponseChange/ResponseChangeDialog';
import { changedNotice } from '../ResponseChange/changedNotice';
import React, { useState } from 'react';

type EntryChangeButtonProps = {
    watch: Watch<EntryViewType>;
};

function showChanged(mode: ResponseChangeModeType, ids: number[]): void {
    navigate(`/entries/${ids[0]}`);
    postNotice(changedNotice(mode, ids.length));
}

const EntryChangeButton: React.FC<EntryChangeButtonProps> = ({ watch }) => {
    const { canOperate } = useSession();
    const [current, setCurrent] = useState<EntryType | null>(null);
    return (
        <>
            <button
                type='button'
                disabled={!canOperate}
                title={canOperate ? undefined : 'Sign in to change responses'}
                onClick={() => setCurrent(watch.value?.entry ?? null)}
            >
                Change response
            </button>
            {current && (
                <ResponseChangeDialog
                    ids={[current.id]}
                    current={current}
                    onChanged={showChanged}
                    onClose={() => setCurrent(null)}
                />
            )}
        </>
    );
};

export default React.memo(EntryChangeButton);
