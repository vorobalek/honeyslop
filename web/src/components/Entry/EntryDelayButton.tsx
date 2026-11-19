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
    if (!canOperate) return null;
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
                className='link danger'
                onClick={open}
            >
                change
            </button>
            {opened && (
                <DelayDialog
                    title='Delay of this entry'
                    question='Change the delay of this entry?'
                    milliseconds={opened.milliseconds}
                    placeholder={String(opened.fallback)}
                    hint={`Empty uses the default, ${opened.fallback}ms now. A delay set on a client wins over it.`}
                    detail={(value) =>
                        `The trap answers from this entry no sooner than ${value ?? opened.fallback}ms after a request.`
                    }
                    notice={(value) =>
                        value === null
                            ? `The entry uses the default ${opened.fallback}ms again.`
                            : `The entry answers no sooner than ${value}ms after a request.`
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
