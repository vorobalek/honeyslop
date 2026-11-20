import { useSession } from '../../hooks/useSession';
import { invokeHub } from '../../lib/hubConnection';
import { noiseLabel, noiseSize } from '../../lib/noise';
import { ClientNoiseChangeType } from '../../types/clientNoiseChangeType';
import { NoiseTierType } from '../../types/noiseTierType';
import NoiseDialog from '../Common/NoiseDialog';
import React, { useState } from 'react';

type ClientNoiseButtonProps = {
    ip: string;
    noise: NoiseTierType | null;
};

const ClientNoiseButton: React.FC<ClientNoiseButtonProps> = ({ ip, noise }) => {
    const { canOperate } = useSession();
    const [opened, setOpened] = useState(false);
    if (!canOperate) return null;
    return (
        <>
            <button
                type='button'
                className='link danger'
                onClick={() => setOpened(true)}
            >
                change
            </button>
            {opened && (
                <NoiseDialog
                    title='Noise of this address'
                    noise={noise}
                    question={(tier) =>
                        tier === null ? `Stop the noise for ${ip}?` : `Answer ${ip} with noise, ${noiseLabel(tier)}?`
                    }
                    detail={(tier) =>
                        tier === null
                            ? 'The trap answers this address as usual again.'
                            : `The trap answers every request of this address with ${noiseSize(tier)} of zero bytes instead of a response.`
                    }
                    notice={(tier) =>
                        tier === null
                            ? 'The address gets ordinary answers again.'
                            : `The address gets noise, ${noiseLabel(tier)}.`
                    }
                    save={async (tier) => {
                        const result = await invokeHub<ClientNoiseChangeType>('setClientNoise', ip, tier);
                        return result.changed ? null : (Object.values(result.errors)[0] ?? 'Not changed.');
                    }}
                    onClose={() => setOpened(false)}
                />
            )}
        </>
    );
};

export default React.memo(ClientNoiseButton);
