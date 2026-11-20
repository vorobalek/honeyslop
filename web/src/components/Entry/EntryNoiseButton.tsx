import { useSession } from '../../hooks/useSession';
import { invokeHub } from '../../lib/hubConnection';
import { noiseLabel, noiseSize } from '../../lib/noise';
import { Watch } from '../../lib/watchStore';
import { EntryChangeType } from '../../types/entryChangeType';
import { EntryViewType } from '../../types/entryViewType';
import { NoiseTierType } from '../../types/noiseTierType';
import NoiseDialog from '../Common/NoiseDialog';
import React, { useState } from 'react';

type EntryNoiseButtonProps = {
    watch: Watch<EntryViewType>;
};

type Opened = {
    id: number;
    noise: NoiseTierType | null;
};

const EntryNoiseButton: React.FC<EntryNoiseButtonProps> = ({ watch }) => {
    const { canOperate } = useSession();
    const [opened, setOpened] = useState<Opened | null>(null);
    if (!canOperate) return null;
    const open = () => {
        const view = watch.value;
        if (view === undefined) return;
        setOpened({ id: view.entry.id, noise: view.entry.noise });
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
                <NoiseDialog
                    title='Noise of this entry'
                    noise={opened.noise}
                    question={(tier) =>
                        tier === null
                            ? 'Stop the noise of this entry?'
                            : `Answer this entry with noise, ${noiseLabel(tier)}?`
                    }
                    detail={(tier) =>
                        tier === null
                            ? 'The trap answers requests for this entry with its response again.'
                            : `The trap answers every request for this entry with ${noiseSize(tier)} of zero bytes instead of its response, unless the client has a noise tier of its own.`
                    }
                    notice={(tier) =>
                        tier === null
                            ? 'The entry answers with its response again.'
                            : `The entry answers with noise, ${noiseLabel(tier)}.`
                    }
                    save={async (tier) => {
                        const result = await invokeHub<EntryChangeType>('setEntryNoise', [opened.id], tier);
                        return result.changed ? null : (Object.values(result.errors)[0] ?? 'Not changed.');
                    }}
                    onClose={() => setOpened(null)}
                />
            )}
        </>
    );
};

export default React.memo(EntryNoiseButton);
