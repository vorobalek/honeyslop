import { useSelectionSize } from '../../hooks/useSelection';
import { useSession } from '../../hooks/useSession';
import { postNotice } from '../../lib/noticeStore';
import { SelectionStore } from '../../lib/selectionStore';
import { ResponseChangeModeType } from '../../types/responseChangeModeType';
import ResponseChangeDialog from '../ResponseChange/ResponseChangeDialog';
import { changedNotice } from '../ResponseChange/changedNotice';
import React, { useState } from 'react';

type ChangeSelectedButtonProps = {
    selection: SelectionStore;
};

const ChangeSelectedButton: React.FC<ChangeSelectedButtonProps> = ({ selection }) => {
    const { canOperate } = useSession();
    const selected = useSelectionSize(selection);
    const [ids, setIds] = useState<number[] | null>(null);
    const showChanged = (mode: ResponseChangeModeType, changed: number[]) => {
        selection.replace([]);
        postNotice(changedNotice(mode, changed.length));
    };
    return (
        <>
            <button
                type='button'
                disabled={!canOperate || selected === 0}
                title={canOperate ? undefined : 'Sign in to change responses'}
                onClick={() => setIds(selection.ids())}
            >
                {selected > 0 ? `Change ${selected} selected` : 'Change selected'}
            </button>
            {ids && (
                <ResponseChangeDialog
                    ids={ids}
                    current={null}
                    onChanged={showChanged}
                    onClose={() => setIds(null)}
                />
            )}
        </>
    );
};

export default React.memo(ChangeSelectedButton);
