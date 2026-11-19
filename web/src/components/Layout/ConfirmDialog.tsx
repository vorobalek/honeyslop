import { ConfirmationRequest, registerConfirmation } from '../../lib/confirmation';
import React, { useEffect, useRef, useState } from 'react';

type Pending = ConfirmationRequest & {
    resolve: (confirmed: boolean) => void;
};

const ConfirmDialog: React.FC = () => {
    const dialog = useRef<HTMLDialogElement>(null);
    const [pending, setPending] = useState<Pending | null>(null);

    useEffect(() => {
        registerConfirmation((request, resolve) => {
            setPending({ ...request, resolve });
            dialog.current?.showModal();
        });
    }, []);

    const close = (value: string) => dialog.current?.close(value);
    const onClose = () => {
        pending?.resolve(dialog.current?.returnValue === 'confirm');
        setPending(null);
    };

    return (
        <dialog
            className='confirm'
            data-confirm-dialog=''
            ref={dialog}
            onClose={onClose}
        >
            <form>
                <p className='confirm-question'>{pending?.question}</p>
                <p className='hint'>{pending?.detail}</p>
                <p className='hint'>{pending?.sender}</p>
                <div className='actions'>
                    <button
                        type='button'
                        value='cancel'
                        autoFocus
                        onClick={() => close('cancel')}
                    >
                        Cancel
                    </button>
                    <button
                        type='button'
                        value='confirm'
                        className='danger-filled'
                        onClick={() => close('confirm')}
                    >
                        {pending?.button ?? 'Confirm'}
                    </button>
                </div>
            </form>
        </dialog>
    );
};

export default React.memo(ConfirmDialog);
