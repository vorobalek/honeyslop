import { useSession } from '../../hooks/useSession';
import { invokeHub } from '../../lib/hubConnection';
import { ClientDelayChangeType } from '../../types/clientDelayChangeType';
import DelayDialog from '../Common/DelayDialog';
import React, { useState } from 'react';

type ClientDelayButtonProps = {
    ip: string;
    milliseconds: number | null;
};

const ClientDelayButton: React.FC<ClientDelayButtonProps> = ({ ip, milliseconds }) => {
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
                <DelayDialog
                    title='Delay of this address'
                    question='Change the delay of this address?'
                    milliseconds={milliseconds}
                    placeholder=''
                    hint='Wins over the delays of the entries and the default. Empty lets them apply again.'
                    detail={(value) =>
                        value === null
                            ? 'The trap goes back to the delays of the entries and the default for this address.'
                            : `The trap answers this address from an entry no sooner than ${value} ms after a request.`
                    }
                    notice={(value) =>
                        value === null
                            ? 'The address gets the delays of the entries again.'
                            : `The address gets answers no sooner than ${value} ms after a request.`
                    }
                    save={async (value) => {
                        const result = await invokeHub<ClientDelayChangeType>('setClientDelay', ip, value);
                        return result.changed ? null : (Object.values(result.errors)[0] ?? 'Not changed.');
                    }}
                    onClose={() => setOpened(false)}
                />
            )}
        </>
    );
};

export default React.memo(ClientDelayButton);
