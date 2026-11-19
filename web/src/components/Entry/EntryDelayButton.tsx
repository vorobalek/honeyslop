import { useSession } from '../../hooks/useSession';
import { timeSpanSeconds } from '../../lib/time';
import { Watch } from '../../lib/watchStore';
import { EntryViewType } from '../../types/entryViewType';
import EntryDelayDialog from './EntryDelayDialog';
import React, { useState } from 'react';

type EntryDelayButtonProps = {
    watch: Watch<EntryViewType>;
};

type Opened = {
    id: number;
    seconds: number | null;
    fallback: number;
};

const EntryDelayButton: React.FC<EntryDelayButtonProps> = ({ watch }) => {
    const { canOperate } = useSession();
    const [opened, setOpened] = useState<Opened | null>(null);
    const open = () => {
        const view = watch.value;
        if (view === undefined) return;
        setOpened({
            id: view.entry.id,
            seconds: view.entry.responseDelaySeconds,
            fallback: timeSpanSeconds(view.defaultResponseDelay),
        });
    };
    return (
        <>
            <button
                type='button'
                disabled={!canOperate}
                title={canOperate ? undefined : 'Sign in to change delays'}
                onClick={open}
            >
                Set delay
            </button>
            {opened && (
                <EntryDelayDialog
                    id={opened.id}
                    seconds={opened.seconds}
                    fallback={opened.fallback}
                    onClose={() => setOpened(null)}
                />
            )}
        </>
    );
};

export default React.memo(EntryDelayButton);
