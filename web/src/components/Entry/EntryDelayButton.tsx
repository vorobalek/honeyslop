import { useSession } from '../../hooks/useSession';
import { invokeHub } from '../../lib/hubConnection';
import { Watch } from '../../lib/watchStore';
import { EntryChangeType } from '../../types/entryChangeType';
import { EntryViewType } from '../../types/entryViewType';
import DelayDialog from '../Common/DelayDialog';
import React, { useState } from 'react';

type EntryDelayButtonProps = {
    watch: Watch<EntryViewType>;
};

type Opened = {
    id: number;
    milliseconds: number | null;
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
            milliseconds: view.entry.responseDelayMilliseconds,
            fallback: view.defaultResponseDelayMilliseconds,
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
                <DelayDialog
                    title='Delay of this entry'
                    question='Change the delay of this entry?'
                    milliseconds={opened.milliseconds}
                    placeholder={String(opened.fallback)}
                    hint={`Empty uses the default, ${opened.fallback} ms now. A delay set on a client wins over it.`}
                    detail={(value) =>
                        `The trap waits ${value ?? opened.fallback} ms before every response it serves from this entry.`
                    }
                    notice={(value) =>
                        value === null
                            ? `The entry waits the default ${opened.fallback} ms again.`
                            : `The entry waits ${value} ms before every response.`
                    }
                    save={async (value) => {
                        const result = await invokeHub<EntryChangeType>('setEntryDelay', [opened.id], value);
                        return result.changed ? null : (Object.values(result.errors)[0] ?? 'Not changed.');
                    }}
                    onClose={() => setOpened(null)}
                />
            )}
        </>
    );
};

export default React.memo(EntryDelayButton);
